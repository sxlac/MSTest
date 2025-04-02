using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NsbEventHandlers;
using NServiceBus.Testing;
using Refit;
using Signify.HBA1CPOC.Messages.Events.Akka;
using Signify.HBA1CPOC.Messages.Events.Status;
using Signify.HBA1CPOC.Svc.Core.ApiClient.Response;
using Signify.HBA1CPOC.Svc.Core.ApiClient;
using Signify.HBA1CPOC.Svc.Core.BusinessRules;
using Signify.HBA1CPOC.Svc.Core.Constants;
using Signify.HBA1CPOC.Svc.Core.Data.Entities;
using Signify.HBA1CPOC.Svc.Core.Exceptions;
using Signify.HBA1CPOC.Svc.Core.Infrastructure.Observability;
using Signify.HBA1CPOC.Svc.Core.Infrastructure;
using Signify.HBA1CPOC.Svc.Core.Queries;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.HBA1CPOC.Svc.Core.Tests.EventHandlers.Nsb;

public class CdiFailedEventHandlerTests
{
    private readonly FakeTransactionSupplier _transactionSupplier = new();
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly IPayableRules _payableRules = A.Fake<IPayableRules>();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();
    private readonly IEvaluationApi _evaluationApi = A.Fake<IEvaluationApi>();
    private readonly IApplicationTime _applicationTime = new ApplicationTime();

    private CdiFailedEventHandler CreateSubject()
        => new(A.Dummy<ILogger<CdiFailedEventHandler>>(), _transactionSupplier, _mediator, _mapper, _payableRules,
            _publishObservability, _evaluationApi, _applicationTime);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_WhenNotPerformed_DoesNothing(bool payProvider)
    {
        // Arrange
        var request = new CDIFailedEvent
        {
            DateTime = DateTimeOffset.UtcNow,
            PayProvider = payProvider
        };

        A.CallTo(() => _mediator.Send(A<GetHBA1CPOC>._, A<CancellationToken>._))
            .Returns(new Core.Data.Entities.HBA1CPOC());

        A.CallTo(() => _mediator.Send(A<QueryExamStatuses>._, A<CancellationToken>._))
            .Returns([
                new HBA1CPOCStatus
                {
                    HBA1CPOCStatusCodeId = HBA1CPOCStatusCode.HBA1CPOCNotPerformed.HBA1CPOCStatusCodeId
                }
            ]);

        var context = new TestableMessageHandlerContext();

        // Act
        await CreateSubject().Handle(request, context);

        // Assert
        _transactionSupplier.AssertNoTransactionCreated();

        Assert.Empty(context.SentMessages);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_WhenExamDoesNotQualifyForProviderPay_Tests(bool payProvider)
    {
        // Arrange
        const int evaluationId = 1;

        var request = new CDIFailedEvent
        {
            EvaluationId = evaluationId,
            DateTime = DateTimeOffset.UtcNow,
            PayProvider = payProvider
        };

        A.CallTo(() => _mediator.Send(A<GetHBA1CPOC>._, A<CancellationToken>._))
            .Returns(new Core.Data.Entities.HBA1CPOC());

        A.CallTo(() => _mediator.Send(A<QueryExamStatuses>._, A<CancellationToken>._))
            .Returns([
                new HBA1CPOCStatus
                {
                    HBA1CPOCStatusCodeId = HBA1CPOCStatusCode.HBA1CPOCPerformed.HBA1CPOCStatusCodeId
                }
            ]);

        var context = new TestableMessageHandlerContext();

        // Act
        await CreateSubject().Handle(request, context);

        // Assert
        _transactionSupplier.AssertCommit();

        Assert.Equal(2, context.SentMessages.Length);

        var statusCode = payProvider
            ? HBA1CPOCStatusCode.CdiFailedWithPayReceived
            : HBA1CPOCStatusCode.CdiFailedWithoutPayReceived;

        AssertSent<ProviderPayStatusEvent>(context,
            filter: message => message.StatusCode == statusCode.HBA1CPOCStatusCodeId,
            message =>
            {
                Assert.Equal(evaluationId, message.EvaluationId);
                Assert.Equal(request.DateTime, message.StatusDateTime);
            });

        if (!payProvider)
        {
            AssertSent<ProviderPayStatusEvent>(context,
                filter: message =>
                    message.StatusCode == HBA1CPOCStatusCode.ProviderNonPayableEventReceived.HBA1CPOCStatusCodeId,
                message =>
                {
                    Assert.Equal(evaluationId, message.EvaluationId);
                    Assert.Equal(request.DateTime, message.StatusDateTime);
                });
        }

        if (payProvider)
            A.CallTo(_payableRules).MustHaveHappened();
        else
            A.CallTo(_payableRules).MustNotHaveHappened();
    }

    private static void AssertSent<T>(TestablePipelineContext context, Func<T, bool> filter, Action<T> action)
    {
        foreach (var message in context.SentMessages)
        {
            if (message.Message is not T asType || !filter(asType))
                continue;

            action(asType);

            return;
        }

        Assert.Fail("Sent message not found");
    }

    public static IEnumerable<object[]> Handle_Exam_NotFound_TestData()
    {
        return CdiEventsCollectionOnSuccess();
    }

    private static IEnumerable<object[]> CdiEventsCollectionOnSuccess()
    {
        yield return [new CDIFailedEvent { PayProvider = true }];
        yield return [new CDIFailedEvent { PayProvider = false }];
    }

    [Theory]
    [MemberData(nameof(Handle_Exam_NotFound_TestData))]
    public async Task Handle_Exam_NotFound_When_Only_Finalized(CDIFailedEvent cdiEvent)
    {
        var apiResponseBody = A.Fake<List<EvaluationStatusHistory>>();
        var status = A.Fake<EvaluationStatusHistory>();
        status.EvaluationStatusCodeId = EvaluationStatus.EvaluationFinalized;
        apiResponseBody.Add(status);
        var apiResponse = new ApiResponse<List<EvaluationStatusHistory>>(new HttpResponseMessage(HttpStatusCode.OK),
            apiResponseBody, null!);
        A.CallTo(() => _mediator.Send(A<GetHBA1CPOC>._, A<CancellationToken>._))
            .Returns(null as Core.Data.Entities.HBA1CPOC);
        A.CallTo(() => _evaluationApi.GetEvaluationStatusHistory(A<long>._)).Returns(apiResponse);

        await Assert.ThrowsAnyAsync<ExamNotFoundException>(async () =>
            await CreateSubject().Handle(cdiEvent, new TestableMessageHandlerContext()));

        A.CallTo(() => _publishObservability.RegisterEvent(
            A<ObservabilityEvent>.That.Matches(e => e.EventType == Observability.Evaluation.MissingEvaluationEvent),
            true)).MustHaveHappenedOnceExactly();
    }

    [Theory]
    [MemberData(nameof(Handle_Exam_NotFound_TestData))]
    public async Task Handle_Exam_NotFound_When_Only_Canceled(CDIFailedEvent cdiEvent)
    {
        var apiResponseBody = A.Fake<List<EvaluationStatusHistory>>();
        var status = A.Fake<EvaluationStatusHistory>();
        status.EvaluationStatusCodeId = EvaluationStatus.EvaluationCanceled;
        apiResponseBody.Add(status);
        var apiResponse = new ApiResponse<List<EvaluationStatusHistory>>(new HttpResponseMessage(HttpStatusCode.OK),
            apiResponseBody, null!);
        var subject = CreateSubject();
        A.CallTo(() => _mediator.Send(A<GetHBA1CPOC>._, A<CancellationToken>._))
            .Returns(null as Core.Data.Entities.HBA1CPOC);
        A.CallTo(() => _evaluationApi.GetEvaluationStatusHistory(A<long>._)).Returns(apiResponse);


        await subject.Handle(cdiEvent, new TestableMessageHandlerContext());

        A.CallTo(() => _publishObservability.RegisterEvent(
            A<ObservabilityEvent>.That.Matches(e => e.EventType == Observability.Evaluation.EvaluationCanceledEvent),
            true)).MustHaveHappenedOnceExactly();
    }

    [Theory]
    [MemberData(nameof(Handle_Exam_NotFound_TestData))]
    public async Task Handle_Exam_NotFound_When_Both_Finalized_And_Canceled(CDIFailedEvent cdiEvent)
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
        A.CallTo(() => _mediator.Send(A<GetHBA1CPOC>._, A<CancellationToken>._))
            .Returns(null as Core.Data.Entities.HBA1CPOC);
        A.CallTo(() => _evaluationApi.GetEvaluationStatusHistory(A<long>._)).Returns(apiResponse);

        await Assert.ThrowsAnyAsync<ExamNotFoundException>(async () =>
            await subject.Handle(cdiEvent, new TestableMessageHandlerContext()));


        A.CallTo(() => _publishObservability.RegisterEvent(
            A<ObservabilityEvent>.That.Matches(e => e.EventType == Observability.Evaluation.MissingEvaluationEvent),
            true)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _publishObservability.RegisterEvent(
            A<ObservabilityEvent>.That.Matches(e => e.EventType == Observability.Evaluation.EvaluationCanceledEvent),
            true)).MustHaveHappenedOnceExactly();
    }

    [Theory]
    [MemberData(nameof(Handle_Exam_NotFound_TestData))]
    public async Task Handle_Exam_NotFound_When_GetVersionHistoryApi_Fails(CDIFailedEvent cdiEvent)
    {
        var apiResponse = new ApiResponse<List<EvaluationStatusHistory>>(
            new HttpResponseMessage(HttpStatusCode.InternalServerError),
            null, null!);
        var subject = CreateSubject();
        A.CallTo(() => _mediator.Send(A<GetHBA1CPOC>._, A<CancellationToken>._))
            .Returns(null as Core.Data.Entities.HBA1CPOC);
        A.CallTo(() => _evaluationApi.GetEvaluationStatusHistory(A<long>._)).Returns(apiResponse);

        await Assert.ThrowsAnyAsync<EvaluationApiRequestException>(async () =>
            await subject.Handle(cdiEvent, new TestableMessageHandlerContext()));

        A.CallTo(() => _publishObservability.RegisterEvent(
                A<ObservabilityEvent>.That.Matches(e =>
                    e.EventType == Observability.ApiIssues.ExternalApiFailureEvent && e.EventValue["Message"].ToString()
                        .Contains("failed with HttpStatusCode:")),
                true))
            .MustHaveHappenedOnceExactly();
    }

    [Theory]
    [MemberData(nameof(Handle_Exam_NotFound_TestData))]
    public async Task Handle_Exam_NotFound_When_GetVersionHistoryApi_Returns_EmptyContent(CDIFailedEvent cdiEvent)
    {
        var apiResponse = new ApiResponse<List<EvaluationStatusHistory>>(new HttpResponseMessage(HttpStatusCode.OK),
            null, null!);
        var subject = CreateSubject();
        A.CallTo(() => _mediator.Send(A<GetHBA1CPOC>._, A<CancellationToken>._))
            .Returns(null as Core.Data.Entities.HBA1CPOC);
        A.CallTo(() => _evaluationApi.GetEvaluationStatusHistory(A<long>._)).Returns(apiResponse);

        await Assert.ThrowsAnyAsync<EvaluationApiRequestException>(async () =>
            await subject.Handle(cdiEvent, new TestableMessageHandlerContext()));


        A.CallTo(() => _publishObservability.RegisterEvent(
                A<ObservabilityEvent>.That.Matches(e =>
                    e.EventType == Observability.ApiIssues.ExternalApiFailureEvent &&
                    e.EventValue["Message"].ToString().Contains("Empty response from")),
                true))
            .MustHaveHappenedOnceExactly();
    }
}