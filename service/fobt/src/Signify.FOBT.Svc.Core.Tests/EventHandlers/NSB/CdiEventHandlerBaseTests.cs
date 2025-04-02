using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NsbEventHandlers;
using NServiceBus.Testing;
using NServiceBus;
using Refit;
using Signify.FOBT.Svc.Core.ApiClient.Response;
using Signify.FOBT.Svc.Core.ApiClient;
using Signify.FOBT.Svc.Core.Commands;
using Signify.FOBT.Svc.Core.Constants;
using Signify.FOBT.Svc.Core.Data.Entities;
using Signify.FOBT.Svc.Core.Data;
using Signify.FOBT.Svc.Core.Events;
using Signify.FOBT.Svc.Core.Exceptions;
using Signify.FOBT.Svc.Core.Infrastructure.Observability;
using Signify.FOBT.Svc.Core.Infrastructure;
using Signify.FOBT.Svc.Core.Queries;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.EventHandlers.Nsb;

public class CdiEventHandlerBaseTests
{
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();
    private readonly FakeTransactionSupplier _transactionSupplier = new();
    private readonly IEvaluationApi _evaluationApi = A.Fake<IEvaluationApi>();
    private static readonly FakeApplicationTime ApplicationTime = new();

    private class ConcreteSubject : CdiEventHandlerBase
    {
        public ConcreteSubject(IMediator mediator, ITransactionSupplier transactionSupplier, IPublishObservability publishObservability,
            IEvaluationApi evaluationApi, IApplicationTime applicationTime)
            : base(A.Dummy<ILogger>(), mediator, A.Dummy<IMapper>(), transactionSupplier, publishObservability, evaluationApi, applicationTime)
        {
        }

        public new Task<Core.Data.Entities.FOBT> GetExam(CdiEventBase message)
            => base.GetExam(message);

        public new static bool IsPerformed(IEnumerable<FOBTStatus> statuses)
            => CdiEventHandlerBase.IsPerformed(statuses);

        public new Task Handle(CdiEventBase message, FOBTStatusCode statusCode, IMessageHandlerContext context)
            => base.Handle(message, statusCode, context);
    }

    private ConcreteSubject CreateSubject()
        => new(_mediator, _transactionSupplier, _publishObservability, _evaluationApi, ApplicationTime);

    [Fact]
    public async Task GetExam_WhenExists_ReturnsExam()
    {
        // Arrange
        const long evaluationId = 1;

        var request = new CDIPassedEvent
        {
            EvaluationId = evaluationId
        };

        A.CallTo(() => _mediator.Send(A<GetFOBT>._, A<CancellationToken>._))
            .Returns(new Core.Data.Entities.FOBT());

        // Act
        var actual = await CreateSubject().GetExam(request);

        // Assert
        Assert.NotNull(actual);

        A.CallTo(() => _mediator.Send(A<GetFOBT>.That.Matches(q =>
                    q.EvaluationId == evaluationId),
                A<CancellationToken>._))
            .MustHaveHappened();
    }

    [Theory]
    [MemberData(nameof(CdiEventsCollection))]
    public async Task Handle_Exam_NotFound_When_Only_Finalized(CdiEventBase cdiEvent, FOBTStatusCode cdiStatus)
    {
        var apiResponseBody = A.Fake<List<EvaluationStatusHistory>>();
        var status = A.Fake<EvaluationStatusHistory>();
        status.EvaluationStatusCodeId = EvaluationStatus.EvaluationFinalized;
        apiResponseBody.Add(status);
        var apiResponse = new ApiResponse<List<EvaluationStatusHistory>>(new HttpResponseMessage(HttpStatusCode.OK),
            apiResponseBody, null!);
        var subject = CreateSubject();
        A.CallTo(() => _mediator.Send(A<GetFOBT>._, A<CancellationToken>._)).Returns(null as Core.Data.Entities.FOBT);
        A.CallTo(() => _evaluationApi.GetEvaluationStatusHistory(A<long>._)).Returns(apiResponse);
        var context = new TestableMessageHandlerContext();

        await Assert.ThrowsAnyAsync<ExamNotFoundException>(async () => await subject.Handle(cdiEvent, cdiStatus, context));

        A.CallTo(() => _publishObservability.RegisterEvent(
            A<ObservabilityEvent>.That.Matches(e => e.EventType == Observability.Evaluation.MissingEvaluationEvent), true)).MustHaveHappenedOnceExactly();
        Assert.Empty(context.SentMessages);
        _transactionSupplier.AssertRollback();
        A.CallTo(() => _publishObservability.Commit()).MustNotHaveHappened();
    }

    [Theory]
    [MemberData(nameof(CdiEventsCollection))]
    public async Task Handle_Exam_NotFound_When_Only_Canceled(CdiEventBase cdiEvent, FOBTStatusCode cdiStatus)
    {
        var apiResponseBody = A.Fake<List<EvaluationStatusHistory>>();
        var status = A.Fake<EvaluationStatusHistory>();
        status.EvaluationStatusCodeId = EvaluationStatus.EvaluationCanceled;
        apiResponseBody.Add(status);
        var apiResponse = new ApiResponse<List<EvaluationStatusHistory>>(new HttpResponseMessage(HttpStatusCode.OK),
            apiResponseBody, null!);
        var subject = CreateSubject();
        var context = new TestableMessageHandlerContext();
        A.CallTo(() => _mediator.Send(A<GetFOBT>._, A<CancellationToken>._)).Returns(null as Core.Data.Entities.FOBT);
        A.CallTo(() => _evaluationApi.GetEvaluationStatusHistory(A<long>._)).Returns(apiResponse);

        await subject.Handle(cdiEvent, cdiStatus, context);

        A.CallTo(() => _publishObservability.RegisterEvent(
            A<ObservabilityEvent>.That.Matches(e => e.EventType == Observability.Evaluation.EvaluationCanceledEvent), true)).MustHaveHappenedOnceExactly();
        Assert.Empty(context.SentMessages);
        _transactionSupplier.AssertCommit();
        A.CallTo(() => _publishObservability.Commit()).MustHaveHappenedOnceExactly();
    }

    [Theory]
    [MemberData(nameof(CdiEventsCollection))]
    public async Task Handle_Exam_NotFound_When_Both_Finalized_And_Canceled(CdiEventBase cdiEvent, FOBTStatusCode cdiStatus)
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
        A.CallTo(() => _mediator.Send(A<GetFOBT>._, A<CancellationToken>._)).Returns(null as Core.Data.Entities.FOBT);
        A.CallTo(() => _evaluationApi.GetEvaluationStatusHistory(A<long>._)).Returns(apiResponse);
        var context = new TestableMessageHandlerContext();

        await Assert.ThrowsAnyAsync<ExamNotFoundException>(async () => await subject.Handle(cdiEvent, cdiStatus, context));

        A.CallTo(() => _publishObservability.RegisterEvent(
            A<ObservabilityEvent>.That.Matches(e => e.EventType == Observability.Evaluation.MissingEvaluationEvent), true)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _publishObservability.RegisterEvent(
            A<ObservabilityEvent>.That.Matches(e => e.EventType == Observability.Evaluation.EvaluationCanceledEvent), true)).MustHaveHappenedOnceExactly();
        Assert.Empty(context.SentMessages);
        _transactionSupplier.AssertRollback();
        A.CallTo(() => _publishObservability.Commit()).MustNotHaveHappened();
    }

    [Theory]
    [MemberData(nameof(CdiEventsCollection))]
    public async Task Handle_Exam_NotFound_When_GetVersionHistoryApi_Fails(CdiEventBase cdiEvent, FOBTStatusCode cdiStatus)
    {
        var apiResponse = new ApiResponse<List<EvaluationStatusHistory>>(new HttpResponseMessage(HttpStatusCode.InternalServerError),
            null, null!);
        var subject = CreateSubject();
        var context = new TestableMessageHandlerContext();
        A.CallTo(() => _mediator.Send(A<GetFOBT>._, A<CancellationToken>._)).Returns(null as Core.Data.Entities.FOBT);
        A.CallTo(() => _evaluationApi.GetEvaluationStatusHistory(A<long>._)).Returns(apiResponse);

        await Assert.ThrowsAnyAsync<EvaluationApiRequestException>(async () => await subject.Handle(cdiEvent, cdiStatus, context));

        A.CallTo(() => _publishObservability.RegisterEvent(
                A<ObservabilityEvent>.That.Matches(e =>
                    e.EventType == Observability.ApiIssues.ExternalApiFailureEvent &&
                    e.EventValue["Message"].ToString().Contains("failed with HttpStatusCode:")),
                true))
            .MustHaveHappenedOnceExactly();
        Assert.Empty(context.SentMessages);
        _transactionSupplier.AssertRollback();
        A.CallTo(() => _publishObservability.Commit()).MustNotHaveHappened();
    }

    [Theory]
    [MemberData(nameof(CdiEventsCollection))]
    public async Task Handle_Exam_NotFound_When_GetVersionHistoryApi_Returns_EmptyContent(CdiEventBase cdiEvent, FOBTStatusCode cdiStatus)
    {
        var apiResponse = new ApiResponse<List<EvaluationStatusHistory>>(new HttpResponseMessage(HttpStatusCode.OK),
            null, null!);
        var subject = CreateSubject();
        var context = new TestableMessageHandlerContext();
        A.CallTo(() => _mediator.Send(A<GetFOBT>._, A<CancellationToken>._)).Returns(null as Core.Data.Entities.FOBT);
        A.CallTo(() => _evaluationApi.GetEvaluationStatusHistory(A<long>._)).Returns(apiResponse);

        await Assert.ThrowsAnyAsync<EvaluationApiRequestException>(async () => await subject.Handle(cdiEvent, cdiStatus, context));

        A.CallTo(() => _publishObservability.RegisterEvent(
                A<ObservabilityEvent>.That.Matches(e =>
                    e.EventType == Observability.ApiIssues.ExternalApiFailureEvent && e.EventValue["Message"].ToString().Contains("Empty response from")),
                true))
            .MustHaveHappenedOnceExactly();
        Assert.Empty(context.SentMessages);
        _transactionSupplier.AssertRollback();
        A.CallTo(() => _publishObservability.Commit()).MustNotHaveHappened();
    }

    [Theory]
    [MemberData(nameof(IsPerformed_TestData))]
    public void IsPerformed_Tests(IEnumerable<FOBTStatusCode> statuses, bool expectedResult)
    {
        // Arrange
        var examStatuses = new List<FOBTStatus>();

        foreach (var status in statuses)
        {
            examStatuses.Add(new FOBTStatus
            {
                FOBTStatusCodeId = status.FOBTStatusCodeId
            });
        }

        // Act
        var actual = ConcreteSubject.IsPerformed(examStatuses);

        // Assert
        Assert.Equal(expectedResult, actual);
    }

    [Fact]
    public async Task Handle_WhenPayable_PublishesProviderPayableEventReceived()
    {
        // Arrange
        var request = new CDIPassedEvent
        {
            EvaluationId = 1,
            RequestId = Guid.NewGuid(),
            DateTime = DateTimeOffset.UtcNow
        };
        var exam = A.Fake<Core.Data.Entities.FOBT>();
        exam.EvaluationId = 1;
        var examStatuses = new List<FOBTStatus>
        {
            new()
            {
                FOBTStatusCodeId = FOBTStatusCode.FOBTPerformed.FOBTStatusCodeId
            },
            new()
            {
                FOBTStatusCodeId = FOBTStatusCode.ValidLabResultsReceived.FOBTStatusCodeId
            }
        };
        var context = new TestableMessageHandlerContext();
        A.CallTo(() => _mediator.Send(A<GetFOBT>._, A<CancellationToken>._))
            .Returns(exam);
        A.CallTo(() => _mediator.Send(A<QueryExamStatuses>._, A<CancellationToken>._))
            .Returns(examStatuses);
        // Act
        await CreateSubject().Handle(request, FOBTStatusCode.CdiPassedReceived, context);

        // Assert
        Assert.Single(context.SentMessages);
        var providerPayRequest = context.FindSentMessage<ProviderPayRequest>();
        Assert.NotNull(providerPayRequest);
        Assert.NotEmpty(providerPayRequest.AdditionalDetails);
    }

    public static IEnumerable<object[]> IsPerformed_TestData()
    {
        yield return
        [
            new List<FOBTStatusCode>
            {
                FOBTStatusCode.FOBTPerformed
            },
            true
        ];

        yield return
        [
            new List<FOBTStatusCode>
            {
                FOBTStatusCode.FOBTNotPerformed
            },
            false
        ];
    }

    /// <summary>
    /// EventType with PayProvider (if applicable), StatusCode, Total number of sent Nsb messages expected
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<object[]> CdiEventsCollection()
    {
        yield return
        [
            new CDIPassedEvent
            {
                EvaluationId = 1,
                RequestId = Guid.NewGuid(),
                DateTime = ApplicationTime.UtcNow()
            },
            FOBTStatusCode.CdiPassedReceived
        ];
        yield return
        [
            new CDIFailedEvent
            {
                EvaluationId = 1,
                RequestId = Guid.NewGuid(),
                DateTime = ApplicationTime.UtcNow(), PayProvider = true
            },
            FOBTStatusCode.CdiFailedWithPayReceived
        ];
        yield return
        [
            new CDIFailedEvent
            {
                EvaluationId = 1,
                RequestId = Guid.NewGuid(),
                DateTime = ApplicationTime.UtcNow(), PayProvider = false
            },
            FOBTStatusCode.CdiFailedWithoutPayReceived
        ];
    }
}