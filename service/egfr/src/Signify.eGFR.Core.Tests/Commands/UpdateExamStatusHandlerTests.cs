using AutoMapper;
using EgfrNsbEvents;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.eGFR.Core.Commands;
using Signify.eGFR.Core.Data.Entities;
using Signify.eGFR.Core.Events.Akka;
using Signify.eGFR.Core.Events.Status;
using Signify.eGFR.Core.Queries;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using Xunit;

namespace Signify.eGFR.Core.Tests.Commands;

public class UpdateExamStatusTests
{
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly ILogger<UpdateExamStatusHandler> _logger = A.Fake<ILogger<UpdateExamStatusHandler>>();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();
    private readonly FakeApplicationTime _fakeApplicationTime = A.Fake<FakeApplicationTime>();

    private UpdateExamStatusHandler CreateSubject()
        => new(_logger, _mapper, _mediator, _publishObservability);

    private ExamStatusEvent CreateProviderPayStatus(ExamStatusCode statusCode, string eventName)
    {
        var status = A.Fake<ProviderPayStatusEvent>();
        status.EventId = Guid.NewGuid();
        status.EvaluationId = 1;
        status.StatusCode = statusCode;
        status.MemberPlanId = 123456;
        status.ProviderId = 456;
        status.ExamId = 1;
        status.StatusDateTime = _fakeApplicationTime.UtcNow();
        status.ParentCdiEvent = eventName;
        status.ParentEventReceivedDateTime = _fakeApplicationTime.UtcNow();
        return status;
    }
    
    private ExamStatusEvent CreateOrderCreatedStatus(ExamStatusCode statusCode)
    {
        var status = A.Fake<OrderRequestedStatusEvent>();
        status.EventId = Guid.NewGuid();
        status.EvaluationId = 1;
        status.StatusCode = statusCode;
        status.MemberPlanId = 123456;
        status.ProviderId = 456;
        status.ExamId = 1;
        status.StatusDateTime = _fakeApplicationTime.UtcNow();
        status.ParentEventReceivedDateTime = _fakeApplicationTime.UtcNow();
        return status;
    }

    private UpdateExamStatus CreateProviderPayExamStatusRequest(ExamStatusCode statusCode, string eventName)
    {
        var request = A.Fake<UpdateExamStatus>();
        request.ExamStatus = CreateProviderPayStatus(statusCode, eventName);
        request.ExamStatus.StatusDateTime = _fakeApplicationTime.LocalNow();
        return request;
    }
    
    private UpdateExamStatus CreateExamStatusRequest(ExamStatusCode statusCode)
    {
        var request = A.Fake<UpdateExamStatus>();
        request.ExamStatus = CreateOrderCreatedStatus(statusCode);
        request.ExamStatus.StatusDateTime = _fakeApplicationTime.LocalNow();
        return request;
    }
    

    [Fact]
    public async Task Handle_AddsExamStatus_ProviderPayableEventReceived()
    {
        var request = CreateProviderPayExamStatusRequest(ExamStatusCode.ProviderPayableEventReceived, nameof(CDIPassedEvent));
        var fakeStatusResponse = A.Fake<AddExamStatusResponse>();
        fakeStatusResponse.Status.CreatedDateTime = _fakeApplicationTime.UtcNow();
        A.CallTo(() => _mediator.Send(A<AddExamStatus>._, A<CancellationToken>._)).Returns(fakeStatusResponse);

        await CreateSubject().Handle(request, A.Dummy<CancellationToken>());

        A.CallTo(() => _mediator.Send(A<AddExamStatus>.That.Matches(a =>
                a.EvaluationId == request.ExamStatus.EvaluationId 
                && a.Status.ExamStatusCodeId == ExamStatusCode.ProviderPayableEventReceived.StatusCodeId &&
                a.Status.StatusDateTime.Offset==TimeSpan.Zero &&
                a.EventId == request.ExamStatus.EventId && a.AlwaysAddStatus),
            A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mapper.Map<ProviderPayableEventReceived>(A<ExamStatusEvent>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>.That.Matches(a =>
                a.EventId == request.ExamStatus.EventId && a.Status.CreatedDate == fakeStatusResponse.Status.CreatedDateTime),
            A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>.That.Matches(c =>
                c.EventType == Constants.Observability.ProviderPay.PayableCdiEvents), false))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Handle_ExamStatus_With_StatusCode_ProviderNonPayableEventReceived()
    {
        var request = CreateProviderPayExamStatusRequest(ExamStatusCode.ProviderNonPayableEventReceived, nameof(CDIPassedEvent));
        var fakeStatusResponse = A.Fake<AddExamStatusResponse>();
        fakeStatusResponse.Status.CreatedDateTime = _fakeApplicationTime.UtcNow();
        A.CallTo(() => _mediator.Send(A<AddExamStatus>._, A<CancellationToken>._)).Returns(fakeStatusResponse);

        await CreateSubject().Handle(request, A.Dummy<CancellationToken>());

        A.CallTo(() => _mediator.Send(A<AddExamStatus>.That.Matches(a =>
                a.EvaluationId == request.ExamStatus.EvaluationId && 
                a.Status.ExamStatusCodeId == ExamStatusCode.ProviderNonPayableEventReceived.StatusCodeId &&
                a.Status.StatusDateTime.Offset==TimeSpan.Zero &&
                a.EventId == request.ExamStatus.EventId && a.AlwaysAddStatus),
            A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mapper.Map<ProviderNonPayableEventReceived>(A<ExamStatusEvent>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>.That.Matches(a =>
                a.EventId == request.ExamStatus.EventId && a.Status.CreatedDate == fakeStatusResponse.Status.CreatedDateTime),
            A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>.That.Matches(c =>
                c.EventType == Constants.Observability.ProviderPay.NonPayableCdiEvents), false))
            .MustHaveHappenedOnceExactly();
    }
    
    [Fact]
    public async Task Handle_ExamStatus_With_StatusCode_ProviderPayRequestSentReceived()
    {
        var request = CreateProviderPayExamStatusRequest(ExamStatusCode.ProviderPayRequestSent, nameof(CDIPassedEvent));
        var fakeStatusResponse = A.Fake<AddExamStatusResponse>();
        fakeStatusResponse.Status.CreatedDateTime = _fakeApplicationTime.UtcNow();
        var fakeProviderPayRequest = A.Fake<ProviderPayRequestSent>();
        fakeProviderPayRequest.ParentEventDateTime = _fakeApplicationTime.UtcNow();
        A.CallTo(() => _mediator.Send(A<AddExamStatus>._, A<CancellationToken>._)).Returns(fakeStatusResponse);
        A.CallTo(() => _mapper.Map<ProviderPayRequestSent>(A<ProviderPayStatusEvent>._))
            .Returns(fakeProviderPayRequest);
        
        await CreateSubject().Handle(request, A.Dummy<CancellationToken>());

        A.CallTo(() => _mediator.Send(A<AddExamStatus>.That.Matches(a =>
                a.EvaluationId == request.ExamStatus.EvaluationId && 
                a.Status.ExamStatusCodeId == ExamStatusCode.ProviderPayRequestSent.StatusCodeId &&
                a.Status.StatusDateTime.Offset==TimeSpan.Zero &&
                a.EventId == request.ExamStatus.EventId && !a.AlwaysAddStatus),
            A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mapper.Map<ProviderPayRequestSent>(A<ProviderPayStatusEvent>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>.That.Matches(a =>
                a.EventId == request.ExamStatus.EventId && a.Status.CreatedDate == fakeStatusResponse.Status.CreatedDateTime
                && ((ProviderPayRequestSent)a.Status).ParentEventDateTime == ((ProviderPayStatusEvent)request.ExamStatus).ParentEventReceivedDateTime
                ),
            A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>.That.Matches(c =>
                c.EventType == Constants.Observability.ProviderPay.PayableCdiEvents), false))
            .MustHaveHappenedOnceExactly();
    }
    
    [Theory]
    [MemberData(nameof(VariousCdiStatusCodes))]
    public async Task Handle_WithMessage_AddsExamStatus_CdiEvents(ExamStatusCode statusCode, string eventName)
    {
        var request = CreateProviderPayExamStatusRequest(statusCode, eventName);
        var fakeStatusResponse = A.Fake<AddExamStatusResponse>();
        fakeStatusResponse.Status.CreatedDateTime = _fakeApplicationTime.UtcNow();
        var fakeProviderPayRequest = A.Fake<ProviderPayRequestSent>();
        fakeProviderPayRequest.ParentEventDateTime = _fakeApplicationTime.UtcNow();
        A.CallTo(() => _mediator.Send(A<AddExamStatus>._, A<CancellationToken>._)).Returns(fakeStatusResponse);
        
        await CreateSubject().Handle(request, A.Dummy<CancellationToken>());

        A.CallTo(() => _mediator.Send(A<AddExamStatus>.That.Matches(a =>
                a.EvaluationId == request.ExamStatus.EvaluationId && 
                a.Status.ExamStatusCodeId == statusCode.StatusCodeId &&
                a.Status.StatusDateTime.Offset==TimeSpan.Zero &&
                a.EventId == request.ExamStatus.EventId && a.AlwaysAddStatus),
            A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>.That.Matches(c =>
                c.EventType == Constants.Observability.ProviderPay.CdiEvents), false))
            .MustHaveHappenedOnceExactly();
    }
    
    [Fact]
    public async Task Handle_WhenDeterminingWhetherToPublishToKafka_HandlesAllStatusCodes()
    {
        A.CallTo(() => _mediator.Send(A<QueryExam>._, default)).Returns(new Exam());
        A.CallTo(() => _mediator.Send(A<AddExamStatus>._, default)).Returns(A.Fake<AddExamStatusResponse>());
        
        foreach (var statusCode in ExamStatusCode.All)
        {
            UpdateExamStatus request;
            if ( statusCode != ExamStatusCode.OrderRequested) 
                request = CreateProviderPayExamStatusRequest(statusCode, nameof(CDIPassedEvent));
            else
                request = CreateExamStatusRequest(statusCode);
            await CreateSubject().Handle(request, default);
        }

        Assert.True(true);
    }

    public static IEnumerable<object[]> VariousCdiStatusCodes()
    {
        yield return [ExamStatusCode.CdiPassedReceived, nameof(CDIPassedEvent)];
        yield return [ExamStatusCode.CdiFailedWithPayReceived, nameof(CDIFailedEvent)];
        yield return [ExamStatusCode.CdiFailedWithoutPayReceived, nameof(CDIFailedEvent)];
    }

    private ExamStatusEvent CreateOrderRequestedStatus(ExamStatusCode statusCode)
    {
        var status = A.Fake<OrderRequestedStatusEvent>();
        status.EventId = Guid.NewGuid();
        status.EvaluationId = 1;
        status.StatusCode = statusCode;
        status.MemberPlanId = 123456;
        status.ProviderId = 456;
        status.ExamId = 1;
        status.StatusDateTime = _fakeApplicationTime.UtcNow();
        status.ParentEventReceivedDateTime = _fakeApplicationTime.UtcNow();
        return status;
    }

    private UpdateExamStatus CreateOrderRequest(ExamStatusCode statusCode)
    {
        var request = A.Fake<UpdateExamStatus>();
        request.ExamStatus = CreateOrderRequestedStatus(statusCode);
        request.ExamStatus.StatusDateTime = _fakeApplicationTime.LocalNow();
        return request;
    }

    [Fact]
    public async Task Handle_AddsExamStatus_OrderRequestedStatusEvent()
    {
        var request = CreateOrderRequest(ExamStatusCode.OrderRequested);
        var fakeStatusResponse = A.Fake<AddExamStatusResponse>();
        fakeStatusResponse.Status.CreatedDateTime = _fakeApplicationTime.UtcNow();
        A.CallTo(() => _mediator.Send(A<AddExamStatus>._, A<CancellationToken>._)).Returns(fakeStatusResponse);

        await CreateSubject().Handle(request, A.Dummy<CancellationToken>());

        A.CallTo(() => _mediator.Send(A<AddExamStatus>.That.Matches(a =>
                a.EvaluationId == request.ExamStatus.EvaluationId
                && a.Status.ExamStatusCodeId == ExamStatusCode.OrderRequested.StatusCodeId &&
                a.Status.StatusDateTime.Offset == TimeSpan.Zero &&
                a.EventId == request.ExamStatus.EventId && !a.AlwaysAddStatus),
            A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>.That.Matches(c =>
                c.EventType == Constants.Observability.OmsOrderCreation.OrderCreationEvents), false))
            .MustHaveHappenedOnceExactly();
    }
}