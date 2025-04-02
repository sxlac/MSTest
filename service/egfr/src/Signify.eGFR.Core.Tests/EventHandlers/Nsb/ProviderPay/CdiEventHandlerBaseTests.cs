using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NsbEventHandlers;
using NServiceBus;
using NServiceBus.Testing;
using Refit;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using Signify.eGFR.Core.ApiClients.EvaluationApi;
using Signify.eGFR.Core.ApiClients.EvaluationApi.Responses;
using Signify.eGFR.Core.Commands;
using Signify.eGFR.Core.Constants;
using Signify.eGFR.Core.Data;
using Signify.eGFR.Core.Data.Entities;
using Signify.eGFR.Core.Events.Akka;
using Signify.eGFR.Core.Exceptions;
using Signify.eGFR.Core.Infrastructure;
using Signify.eGFR.Core.Models;
using Signify.eGFR.Core.Queries;
using Xunit;

namespace Signify.eGFR.Core.Tests.EventHandlers.Nsb.ProviderPay;

public class CdiEventHandlerBaseTests
{
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();
    private readonly FakeTransactionSupplier _transactionSupplier = new();
    private readonly IEvaluationApi _evaluationApi = A.Fake<IEvaluationApi>();
    private readonly TestableInvokeHandlerContext _messageSession = new();

    private class ConcreteSubject(
        IMediator mediator,
        ITransactionSupplier transactionSupplier,
        IPublishObservability publishObservability,
        IEvaluationApi evaluationApi)
        : CdiEventHandlerBase(A.Dummy<ILogger>(), mediator, transactionSupplier,
            publishObservability, new ApplicationTime(), A.Dummy<IMapper>(), evaluationApi)
    {
        public Task<Exam> GetExam(CdiEventBase message)
            => base.GetExam(message, CancellationToken.None);

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

        A.CallTo(() => _mediator.Send(A<QueryExamByEvaluation>._, A<CancellationToken>._))
            .Returns(new Exam());

        // Act
        var actual = await CreateSubject().GetExam(request);

        // Assert
        Assert.NotNull(actual);
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
    public async Task Handle_WhenPayable_PublishesProviderPayableEventReceivedQuest()
    {
        // Arrange
        var request = new CDIPassedEvent
        {
            EvaluationId = 1,
            RequestId = Guid.NewGuid(),
            DateTime = DateTimeOffset.UtcNow
        };
        var exam = A.Fake<Exam>();
        exam.EvaluationId = 1;
        exam.ExamStatuses = new List<ExamStatus>
        {
            new()
            {
                ExamStatusCodeId = (int)StatusCode.ExamPerformed
            }
        };
        var context = new TestableMessageHandlerContext();
        A.CallTo(() => _mediator.Send(A<QueryExamByEvaluation>._, A<CancellationToken>._))
            .Returns(exam);
        A.CallTo(() => _mediator.Send(A<QueryLabResultByEvaluationId>._, A<CancellationToken>._)).Returns((Data.Entities.LabResult)null);
        A.CallTo(() => _mediator.Send(A<QueryQuestLabResultByEvaluationId>._, A<CancellationToken>._)).Returns(A.Dummy<QuestLabResult>());

        // Act
        await CreateSubject().Handle(request, ExamStatusCode.CdiPassedReceived, context);

        A.CallTo(() => _mediator.Send(A<UpdateExamStatus>.That.Matches(s => s.ExamStatus.StatusCode == ExamStatusCode.CdiPassedReceived), A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<QueryLabResultByEvaluationId>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<QueryQuestLabResultByEvaluationId>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        var sentMessage = context.FindSentMessage<ProviderPayRequest>();
        Assert.Single(context.SentMessages);
        Assert.NotNull(sentMessage);
        _transactionSupplier.AssertCommit();
    }
    
    [Fact]
    public async Task Handle_WhenPayable_PublishesProviderPayableEventReceivedKed()
    {
        // Arrange
        var request = new CDIPassedEvent
        {
            EvaluationId = 1,
            RequestId = Guid.NewGuid(),
            DateTime = DateTimeOffset.UtcNow
        };
        var exam = A.Fake<Exam>();
        exam.EvaluationId = 1;
        exam.ExamStatuses = new List<ExamStatus>
        {
            new()
            {
                ExamStatusCodeId = (int)StatusCode.ExamPerformed
            }
        };
        var context = new TestableMessageHandlerContext();
        A.CallTo(() => _mediator.Send(A<QueryExamByEvaluation>._, A<CancellationToken>._))
            .Returns(exam);
        A.CallTo(() => _mediator.Send(A<QueryLabResultByEvaluationId>._, A<CancellationToken>._)).Returns(A.Dummy<Data.Entities.LabResult>());
        A.CallTo(() => _mediator.Send(A<QueryQuestLabResultByEvaluationId>._, A<CancellationToken>._)).Returns(A.Dummy<QuestLabResult>());

        // Act
        await CreateSubject().Handle(request, ExamStatusCode.CdiPassedReceived, context);

        A.CallTo(() => _mediator.Send(A<UpdateExamStatus>.That.Matches(s => s.ExamStatus.StatusCode == ExamStatusCode.CdiPassedReceived), A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<QueryLabResultByEvaluationId>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<QueryQuestLabResultByEvaluationId>._, A<CancellationToken>._))
            .MustNotHaveHappened();
        var sentMessage = context.FindSentMessage<ProviderPayRequest>();
        Assert.Single(context.SentMessages);
        Assert.NotNull(sentMessage);
        _transactionSupplier.AssertCommit();
    }

    public static IEnumerable<object[]> Handle_Exam_NotFound_TestData() => CdiEventsCollectionOnSuccess();

    private static IEnumerable<object[]> CdiEventsCollectionOnSuccess()
    {
        yield return [new CDIPassedEvent(), ExamStatusCode.CdiPassedReceived];
        yield return [new CDIFailedEvent { PayProvider = true }, ExamStatusCode.CdiFailedWithPayReceived];
        yield return [new CDIFailedEvent { PayProvider = false }, ExamStatusCode.CdiFailedWithoutPayReceived];
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
        A.CallTo(() => _mediator.Send(A<QueryExamByEvaluation>._, A<CancellationToken>._)).Returns(null as Exam);
        A.CallTo(() => _evaluationApi.GetEvaluationStatusHistory(A<long>._)).Returns(apiResponse);

        if (cdiEvent.GetType() == typeof(CDIPassedEvent))
        {
            await Assert.ThrowsAnyAsync<ExamNotFoundByEvaluationException>(async () => await CreateSubject().Handle((CDIPassedEvent)cdiEvent, statusCode,  _messageSession));
        }
        else
        {
            await Assert.ThrowsAnyAsync<ExamNotFoundByEvaluationException>(async () => await CreateSubject().Handle((CDIFailedEvent)cdiEvent, statusCode, _messageSession));
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
        A.CallTo(() => _mediator.Send(A<QueryExamByEvaluation>._, A<CancellationToken>._)).Returns(null as Exam);
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
        A.CallTo(() => _mediator.Send(A<QueryExamByEvaluation>._, A<CancellationToken>._)).Returns(null as Exam);
        A.CallTo(() => _evaluationApi.GetEvaluationStatusHistory(A<long>._)).Returns(apiResponse);

        if (cdiEvent.GetType() == typeof(CDIPassedEvent))
        {
            await Assert.ThrowsAnyAsync<ExamNotFoundByEvaluationException>(async () => await subject.Handle((CDIPassedEvent)cdiEvent, statusCode, _messageSession));
        }
        else
        {
            await Assert.ThrowsAnyAsync<ExamNotFoundByEvaluationException>(async () => await subject.Handle((CDIFailedEvent)cdiEvent, statusCode, _messageSession));
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
        A.CallTo(() => _mediator.Send(A<QueryExamByEvaluation>._, A<CancellationToken>._)).Returns(null as Exam);
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
        A.CallTo(() => _mediator.Send(A<QueryExamByEvaluation>._, A<CancellationToken>._)).Returns(null as Exam);
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
}