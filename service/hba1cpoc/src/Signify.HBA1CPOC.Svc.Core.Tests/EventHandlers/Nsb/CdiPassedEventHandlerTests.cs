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
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.HBA1CPOC.Svc.Core.Tests.EventHandlers.Nsb;

public class CdiPassedEventHandlerTests
{
    private readonly FakeTransactionSupplier _transactionSupplier = new();
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly IPayableRules _payableRules = A.Fake<IPayableRules>();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();
    private readonly IEvaluationApi _evaluationApi = A.Fake<IEvaluationApi>();
    private readonly IApplicationTime _applicationTime = new ApplicationTime();

    private CdiPassedEventHandler CreateSubject()
        => new(A.Dummy<ILogger<CdiPassedEventHandler>>(), _transactionSupplier, _mediator, _mapper, _payableRules,
            _publishObservability, _evaluationApi, _applicationTime);

    [Fact]
    public async Task Handle_WhenNotPerformed_DoesNothing()
    {
        // Arrange
        var request = new CDIPassedEvent
        {
            DateTime = DateTimeOffset.UtcNow
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

    [Fact]
    public async Task Handle_WhenPerformed_PublishesCdiPassedReceived()
    {
        // Arrange
        const int evaluationId = 1;

        var message = new CDIPassedEvent
        {
            EvaluationId = evaluationId,
            DateTime = DateTimeOffset.UtcNow
        };

        A.CallTo(() => _mediator.Send(A<GetHBA1CPOC>._, A<CancellationToken>._))
            .Returns(new Core.Data.Entities.HBA1CPOC
            {
                EvaluationId = evaluationId
            });

        A.CallTo(() => _mediator.Send(A<QueryExamStatuses>._, A<CancellationToken>._))
            .Returns([
                new HBA1CPOCStatus
                {
                    HBA1CPOCStatusCodeId = HBA1CPOCStatusCode.HBA1CPOCPerformed.HBA1CPOCStatusCodeId
                }
            ]);

        var context = new TestableMessageHandlerContext();

        // Act
        await CreateSubject().Handle(message, context);

        // Assert
        _transactionSupplier.AssertCommit();

        var cdiPassedEvent = context.SentMessages.SingleOrDefault(each =>
                each.Message is ProviderPayStatusEvent e &&
                e.StatusCode == HBA1CPOCStatusCode.CdiPassedReceived.HBA1CPOCStatusCodeId)
            ?.Message<ProviderPayStatusEvent>();

        Assert.NotNull(cdiPassedEvent);
        Assert.Equal(evaluationId, cdiPassedEvent.EvaluationId);
        Assert.Equal(message.DateTime, cdiPassedEvent.StatusDateTime);
    }

    public static IEnumerable<object[]> Handle_Exam_NotFound_TestData()
    {
        return CdiEventsCollectionOnSuccess();
    }

    private static IEnumerable<object[]> CdiEventsCollectionOnSuccess()
    {
         yield return new object[] { new CDIPassedEvent()};
    }

    [Theory]
    [MemberData(nameof(Handle_Exam_NotFound_TestData))]
    public async Task Handle_Exam_NotFound_When_Only_Finalized(CDIPassedEvent cdiEvent)
    {
        var apiResponseBody = A.Fake<List<EvaluationStatusHistory>>();
        var status = A.Fake<EvaluationStatusHistory>();
        status.EvaluationStatusCodeId = EvaluationStatus.EvaluationFinalized;
        apiResponseBody.Add(status);
        var apiResponse = new ApiResponse<List<EvaluationStatusHistory>>(new HttpResponseMessage(HttpStatusCode.OK),
            apiResponseBody, null!);
        A.CallTo(() => _mediator.Send(A<GetHBA1CPOC>._, A<CancellationToken>._))
            .Returns(null as Signify.HBA1CPOC.Svc.Core.Data.Entities.HBA1CPOC);
        A.CallTo(() => _evaluationApi.GetEvaluationStatusHistory(A<long>._)).Returns(apiResponse);

        await Assert.ThrowsAnyAsync<ExamNotFoundException>(async () =>
            await CreateSubject().Handle(cdiEvent, new TestableMessageHandlerContext()));

        A.CallTo(() => _publishObservability.RegisterEvent(
            A<ObservabilityEvent>.That.Matches(e => e.EventType == Observability.Evaluation.MissingEvaluationEvent),
            true)).MustHaveHappenedOnceExactly();
    }

    [Theory]
    [MemberData(nameof(Handle_Exam_NotFound_TestData))]
    public async Task Handle_Exam_NotFound_When_Only_Canceled(CDIPassedEvent cdiEvent)
    {
        var apiResponseBody = A.Fake<List<EvaluationStatusHistory>>();
        var status = A.Fake<EvaluationStatusHistory>();
        status.EvaluationStatusCodeId = EvaluationStatus.EvaluationCanceled;
        apiResponseBody.Add(status);
        var apiResponse = new ApiResponse<List<EvaluationStatusHistory>>(new HttpResponseMessage(HttpStatusCode.OK),
            apiResponseBody, null!);
        var subject = CreateSubject();
        A.CallTo(() => _mediator.Send(A<GetHBA1CPOC>._, A<CancellationToken>._))
            .Returns(null as Signify.HBA1CPOC.Svc.Core.Data.Entities.HBA1CPOC);
        A.CallTo(() => _evaluationApi.GetEvaluationStatusHistory(A<long>._)).Returns(apiResponse);


        await subject.Handle(cdiEvent, new TestableMessageHandlerContext());

        A.CallTo(() => _publishObservability.RegisterEvent(
            A<ObservabilityEvent>.That.Matches(e => e.EventType == Observability.Evaluation.EvaluationCanceledEvent),
            true)).MustHaveHappenedOnceExactly();
    }

    [Theory]
    [MemberData(nameof(Handle_Exam_NotFound_TestData))]
    public async Task Handle_Exam_NotFound_When_Both_Finalized_And_Canceled(CDIPassedEvent cdiEvent)
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
            .Returns(null as Signify.HBA1CPOC.Svc.Core.Data.Entities.HBA1CPOC);
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
    public async Task Handle_Exam_NotFound_When_GetVersionHistoryApi_Fails(CDIPassedEvent cdiEvent)
    {
        var apiResponse = new ApiResponse<List<EvaluationStatusHistory>>(
            new HttpResponseMessage(HttpStatusCode.InternalServerError),
            null, null!);
        var subject = CreateSubject();
        A.CallTo(() => _mediator.Send(A<GetHBA1CPOC>._, A<CancellationToken>._))
            .Returns(null as Signify.HBA1CPOC.Svc.Core.Data.Entities.HBA1CPOC);
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
    public async Task Handle_Exam_NotFound_When_GetVersionHistoryApi_Returns_EmptyContent(CDIPassedEvent cdiEvent)
    {
        var apiResponse = new ApiResponse<List<EvaluationStatusHistory>>(new HttpResponseMessage(HttpStatusCode.OK),
            null, null!);
        var subject = CreateSubject();
        A.CallTo(() => _mediator.Send(A<GetHBA1CPOC>._, A<CancellationToken>._))
            .Returns(null as Signify.HBA1CPOC.Svc.Core.Data.Entities.HBA1CPOC);
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