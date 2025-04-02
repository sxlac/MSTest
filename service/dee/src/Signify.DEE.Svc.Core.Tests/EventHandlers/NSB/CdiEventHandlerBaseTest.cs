using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NsbEventHandlers;
using NServiceBus.Testing;
using NServiceBus;
using Refit;
using Signify.DEE.Svc.Core.ApiClient.Responses;
using Signify.DEE.Svc.Core.ApiClient;
using Signify.DEE.Svc.Core.Commands;
using Signify.DEE.Svc.Core.Constants;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Events;
using Signify.DEE.Svc.Core.Exceptions;
using Signify.DEE.Svc.Core.Infrastructure;
using Signify.DEE.Svc.Core.Messages.Queries;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using System;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.EventHandlers.Nsb;

public class CdiEventHandlerBaseTest
{
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();
    private readonly FakeTransactionSupplier _transactionSupplier = new();
    private readonly FakeApplicationTime _applicationTime = new();
    private readonly IEvaluationApi _evaluationApi = A.Fake<IEvaluationApi>();
    private readonly TestableInvokeHandlerContext _messageSession = new();

    private class ConcreteSubject : CdiEventHandlerBase
    {
        public ConcreteSubject(IMediator mediator, ITransactionSupplier transactionSupplier, IPublishObservability publishObservability, IEvaluationApi evaluationApi)
            : base(A.Dummy<ILogger>(), mediator, A.Dummy<IMapper>(), transactionSupplier, publishObservability, evaluationApi, new ApplicationTime())
        {
        }

        public new Task<Exam> GetExam(CdiEventBase message)
            => base.GetExam(message);

        public new static bool IsPerformed(Exam exam)
            => CdiEventHandlerBase.IsPerformed(exam);

        public new Task Handle(CdiEventBase message, ExamStatusCode statusCode, IMessageHandlerContext context)
            => base.Handle(message, statusCode, context);
    }

    private ConcreteSubject CreateSubject()
        => new(_mediator, _transactionSupplier, _publishObservability, _evaluationApi);

    [Fact]
    public async Task GetExam_WhenExists_ReturnsExam()
    {
        // Arrange
        const long evaluationId = 1;

        var request = new CDIPassedEvent
        {
            EvaluationId = evaluationId
        };

        A.CallTo(() => _mediator.Send(A<GetExamByEvaluation>._, A<CancellationToken>._))
            .Returns(new Exam());

        // Act
        var actual = await CreateSubject().GetExam(request);

        // Assert
        Assert.NotNull(actual);

        A.CallTo(() => _mediator.Send(A<GetExamByEvaluation>.That.Matches(q =>
                    q.EvaluationId == evaluationId && q.IncludeStatuses),
                A<CancellationToken>._))
            .MustHaveHappened();
    }

    [Theory]
    [MemberData(nameof(IsPerformed_TestData))]
    public void IsPerformed_Tests(IEnumerable<ExamStatusCode> statuses, bool expectedResult)
    {
        // Arrange
        var exam = new Exam();

        foreach (var status in statuses)
        {
            exam.ExamStatuses.Add(new ExamStatus
            {
                ExamStatusCodeId = status.ExamStatusCodeId
            });
        }

        // Act
        var actual = ConcreteSubject.IsPerformed(exam);

        // Assert
        Assert.Equal(expectedResult, actual);
    }

    [Fact]
    public void IsPerformed_WhenNoStatusExists_Throws()
    {
        // Arrange
        var exam = new Exam();

        // Act
        // Assert
        Assert.ThrowsAny<Exception>(() => ConcreteSubject.IsPerformed(exam));
    }

    [Fact]
    public async Task Handle_WhenPayable_PublishesProviderPayableEventReceived()
    {
        // Arrange
        var request = new CDIPassedEvent
        {
            EvaluationId = 1,
            RequestId = Guid.NewGuid(),
            DateTime = _applicationTime.UtcNow()
        };
        var exam = A.Fake<Exam>();
        exam.EvaluationId = 1;
        exam.ExamStatuses = new List<ExamStatus>
        {
            new()
            {
                ExamStatusCodeId = ExamStatusCode.Performed.ExamStatusCodeId
            }
        };
        var context = new TestableMessageHandlerContext();
        A.CallTo(() => _mediator.Send(A<GetExamByEvaluation>._, A<CancellationToken>._))
            .Returns(exam);
        // Act
        await CreateSubject().Handle(request, ExamStatusCode.CdiPassedReceived, context);

        // Assert
        Assert.Single(context.SentMessages);
        var providerPayRequest = context.FindSentMessage<ProviderPayRequest>();
        Assert.NotNull(providerPayRequest);
        Assert.NotEmpty(providerPayRequest.AdditionalDetails);
    }
    
    public static IEnumerable<object[]> Handle_Exam_NotFound_TestData()
    {
        return CdiEventsCollectionOnSuccess();
    }

    public static IEnumerable<object[]> CdiEventsCollectionOnSuccess()
    {
        yield return new object[] { new CDIPassedEvent(), ExamStatusCode.CdiPassedReceived };
        yield return new object[] { new CDIFailedEvent { PayProvider = true }, ExamStatusCode.CdiFailedWithPayReceived };
        yield return new object[] { new CDIFailedEvent { PayProvider = false }, ExamStatusCode.CdiFailedWithoutPayReceived };
    }

    [Theory]
    [MemberData(nameof(Handle_Exam_NotFound_TestData))]
    public async Task Handle_Exam_NotFound_When_Only_Finalized(CdiEventBase cdiEvent, ExamStatusCode statusCode)
    {
        var apiResponseBody = A.Fake<List<EvaluationStatusHistory>>();
        var status = A.Fake<EvaluationStatusHistory>();
        status.EvaluationStatusCodeId = EvaluationStatus.EvaluationFinalized;
        apiResponseBody.Add(status);
        var apiResponse = new ApiResponse<List<EvaluationStatusHistory>>(new HttpResponseMessage(HttpStatusCode.OK),
            apiResponseBody, null!);
        A.CallTo(() => _mediator.Send(A<GetExamByEvaluation>._, A<CancellationToken>._)).Returns(null as Exam);
        A.CallTo(() => _evaluationApi.GetEvaluationStatusHistory(A<long>._)).Returns(apiResponse);

        if (cdiEvent.GetType() == typeof(CDIPassedEvent))
        {
            await Assert.ThrowsAnyAsync<ExamNotFoundException>(async () => await CreateSubject().Handle((CDIPassedEvent)cdiEvent, statusCode,  _messageSession));
        }
        else
        {
            await Assert.ThrowsAnyAsync<ExamNotFoundException>(async () => await CreateSubject().Handle((CDIFailedEvent)cdiEvent, statusCode, _messageSession));
        }

        A.CallTo(() => _publishObservability.RegisterEvent(
            A<ObservabilityEvent>.That.Matches(e => e.EventType == Observability.Evaluation.MissingEvaluationEvent), true)).MustHaveHappenedOnceExactly();
    }

    [Theory]
    [MemberData(nameof(Handle_Exam_NotFound_TestData))]
    public async Task Handle_Exam_NotFound_When_Only_Canceled(CdiEventBase cdiEvent, ExamStatusCode statusCode)
    {
        var apiResponseBody = A.Fake<List<EvaluationStatusHistory>>();
        var status = A.Fake<EvaluationStatusHistory>();
        status.EvaluationStatusCodeId = EvaluationStatus.EvaluationCanceled;
        apiResponseBody.Add(status);
        var apiResponse = new ApiResponse<List<EvaluationStatusHistory>>(new HttpResponseMessage(HttpStatusCode.OK),
            apiResponseBody, null!);
        var subject = CreateSubject();
        A.CallTo(() => _mediator.Send(A<GetExamByEvaluation>._, A<CancellationToken>._)).Returns(null as Exam);
        A.CallTo(() => _evaluationApi.GetEvaluationStatusHistory(A<long>._)).Returns(apiResponse);

        if (cdiEvent.GetType() == typeof(CDIPassedEvent))
        {
            await subject.Handle((CDIPassedEvent)cdiEvent, statusCode, _messageSession);
        }
        else
        {
            await subject.Handle((CDIFailedEvent)cdiEvent, statusCode, _messageSession);
        }

        A.CallTo(() => _publishObservability.RegisterEvent(
            A<ObservabilityEvent>.That.Matches(e => e.EventType == Observability.Evaluation.EvaluationCanceledEvent), true)).MustHaveHappenedOnceExactly();
    }

    [Theory]
    [MemberData(nameof(Handle_Exam_NotFound_TestData))]
    public async Task Handle_Exam_NotFound_When_Both_Finalized_And_Canceled(CdiEventBase cdiEvent, ExamStatusCode statusCode)
    {
        var apiResponseBody = A.Fake<List<EvaluationStatusHistory>>();
        var status1 = A.Fake<EvaluationStatusHistory>();
        status1.EvaluationStatusCodeId = EvaluationStatus.EvaluationFinalized;
        apiResponseBody.Add(status1);
        var status2 = A.Fake<EvaluationStatusHistory>();
        status2.EvaluationStatusCodeId = EvaluationStatus.EvaluationCanceled;
        apiResponseBody.Add(status2);
        var apiResponse = new ApiResponse<List<EvaluationStatusHistory>>(new HttpResponseMessage(HttpStatusCode.OK),
            apiResponseBody, null!);
        var subject = CreateSubject();
        A.CallTo(() => _mediator.Send(A<GetExamByEvaluation>._, A<CancellationToken>._)).Returns(null as Exam);
        A.CallTo(() => _evaluationApi.GetEvaluationStatusHistory(A<long>._)).Returns(apiResponse);

        if (cdiEvent.GetType() == typeof(CDIPassedEvent))
        {
            await Assert.ThrowsAnyAsync<ExamNotFoundException>(async () => await subject.Handle((CDIPassedEvent)cdiEvent, statusCode, _messageSession));
        }
        else
        {
            await Assert.ThrowsAnyAsync<ExamNotFoundException>(async () => await subject.Handle((CDIFailedEvent)cdiEvent, statusCode, _messageSession));
        }

        A.CallTo(() => _publishObservability.RegisterEvent(
            A<ObservabilityEvent>.That.Matches(e => e.EventType == Observability.Evaluation.MissingEvaluationEvent), true)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _publishObservability.RegisterEvent(
            A<ObservabilityEvent>.That.Matches(e => e.EventType == Observability.Evaluation.EvaluationCanceledEvent), true)).MustHaveHappenedOnceExactly();
    }

    [Theory]
    [MemberData(nameof(Handle_Exam_NotFound_TestData))]
    public async Task Handle_Exam_NotFound_When_GetVersionHistoryApi_Fails(CdiEventBase cdiEvent, ExamStatusCode statusCode)
    {
        var apiResponse = new ApiResponse<List<EvaluationStatusHistory>>(new HttpResponseMessage(HttpStatusCode.InternalServerError),
            null, null!);
        var subject = CreateSubject();
        A.CallTo(() => _mediator.Send(A<GetExamByEvaluation>._, A<CancellationToken>._)).Returns(null as Exam);
        A.CallTo(() => _evaluationApi.GetEvaluationStatusHistory(A<long>._)).Returns(apiResponse);

        if (cdiEvent.GetType() == typeof(CDIPassedEvent))
        {
            await Assert.ThrowsAnyAsync<EvaluationApiRequestException>(async () => await subject.Handle((CDIPassedEvent)cdiEvent, statusCode, _messageSession));
        }
        else
        {
            await Assert.ThrowsAnyAsync<EvaluationApiRequestException>(async () => await subject.Handle((CDIFailedEvent)cdiEvent, statusCode, _messageSession));
        }

        A.CallTo(() => _publishObservability.RegisterEvent(
                A<ObservabilityEvent>.That.Matches(e =>
                    e.EventType == Observability.ApiIssues.ExternalApiFailureEvent && e.EventValue["Message"].ToString().Contains("failed with HttpStatusCode:")),
                true))
            .MustHaveHappenedOnceExactly();
    }

    [Theory]
    [MemberData(nameof(Handle_Exam_NotFound_TestData))]
    public async Task Handle_Exam_NotFound_When_GetVersionHistoryApi_Returns_EmptyContent(CdiEventBase cdiEvent, ExamStatusCode statusCode)
    {
        var apiResponse = new ApiResponse<List<EvaluationStatusHistory>>(new HttpResponseMessage(HttpStatusCode.OK),
            null, null!);
        var subject = CreateSubject();
        A.CallTo(() => _mediator.Send(A<GetExamByEvaluation>._, A<CancellationToken>._)).Returns(null as Exam);
        A.CallTo(() => _evaluationApi.GetEvaluationStatusHistory(A<long>._)).Returns(apiResponse);

        if (cdiEvent.GetType() == typeof(CDIPassedEvent))
        {
            await Assert.ThrowsAnyAsync<EvaluationApiRequestException>(async () => await subject.Handle((CDIPassedEvent)cdiEvent, statusCode, _messageSession));
        }
        else
        {
            await Assert.ThrowsAnyAsync<EvaluationApiRequestException>(async () => await subject.Handle((CDIFailedEvent)cdiEvent, statusCode, _messageSession));
        }

        A.CallTo(() => _publishObservability.RegisterEvent(
                A<ObservabilityEvent>.That.Matches(e =>
                    e.EventType == Observability.ApiIssues.ExternalApiFailureEvent && e.EventValue["Message"].ToString().Contains("Empty response from")),
                true))
            .MustHaveHappenedOnceExactly();
    }

    public static IEnumerable<object[]> IsPerformed_TestData()
    {
        yield return new object[]
        {
            new List<ExamStatusCode>
            {
                ExamStatusCode.Performed
            },
            true
        };

        yield return new object[]
        {
            new List<ExamStatusCode>
            {
                ExamStatusCode.NotPerformed
            },
            false
        };
    }
}