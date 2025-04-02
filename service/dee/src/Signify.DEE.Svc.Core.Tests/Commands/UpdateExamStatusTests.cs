using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.DEE.Messages.Status;
using Signify.DEE.Svc.Core.Commands;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Events;
using Signify.DEE.Svc.Core.Events.Status;
using Signify.DEE.Svc.Core.Messages.Commands;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Commands;

public class UpdateExamStatusTests
{
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly ILogger<UpdateExamStatusHandler> _logger = A.Fake<ILogger<UpdateExamStatusHandler>>();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();

    private UpdateExamStatusHandler CreateSubject()
        => new(_logger, _mapper, _mediator, _publishObservability);

    private static UpdateExamStatus CreateRequest(ExamStatusCode status, string eventName)
    {
        const long evaluationId = 1;
        var request = A.Fake<UpdateExamStatus>();
        request.ExamStatus = A.Fake<ProviderPayStatusEvent>();
        request.ExamStatus.EventId = Guid.NewGuid();
        request.ExamStatus.EvaluationId = evaluationId;
        request.ExamStatus.StatusCode = status;
        ((ProviderPayStatusEvent)request.ExamStatus).ParentCdiEvent = eventName;
        return request;
    }

    [Fact]
    public async Task Handle_WithMessage_AddsExamStatus_ProviderPayableEventReceived()
    {
        var request = CreateRequest(ExamStatusCode.ProviderPayableEventReceived, nameof(CDIPassedEvent));

        await CreateSubject().Handle(request, A.Dummy<CancellationToken>());
        
        A.CallTo(() => _mapper.Map<ProviderPayableEventReceived>(A<ProviderPayStatusEvent>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreateStatus>.That.Matches(a =>
                    a.ExamId == request.ExamStatus.ExamId && a.ExamStatusCode == ExamStatusCode.ProviderPayableEventReceived),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>.That.Matches(c =>
                c.EventType == Constants.Observability.ProviderPay.PayableCdiEvents), false))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Handle_ExamStatus_With_StatusCode_ProviderNonPayableEventReceived()
    {
        var request = CreateRequest(ExamStatusCode.ProviderNonPayableEventReceived, nameof(CDIPassedEvent));
    
        await CreateSubject().Handle(request, A.Dummy<CancellationToken>());
        
        A.CallTo(() => _mapper.Map<ProviderNonPayableEventReceived>(A<ProviderPayStatusEvent>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreateStatus>.That.Matches(a =>
                    a.ExamId == request.ExamStatus.ExamId && a.ExamStatusCode == ExamStatusCode.ProviderNonPayableEventReceived),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>.That.Matches(c =>
                c.EventType == Constants.Observability.ProviderPay.NonPayableCdiEvents), false))
            .MustHaveHappenedOnceExactly();
    }
    
    [Fact]
    public async Task Handle_ExamStatus_With_StatusCode_ProviderPayRequestSentReceived()
    {
        var request = CreateRequest(ExamStatusCode.ProviderPayRequestSent, nameof(CDIPassedEvent));
    
        await CreateSubject().Handle(request, A.Dummy<CancellationToken>());
    
        A.CallTo(() => _mapper.Map<ProviderPayRequestSent>(A<ProviderPayStatusEvent>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreateStatus>.That.Matches(a =>
                    a.ExamId == request.ExamStatus.ExamId && a.ExamStatusCode == ExamStatusCode.ProviderPayRequestSent),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>.That.Matches(c =>
                c.EventType == Constants.Observability.ProviderPay.PayableCdiEvents), false))
            .MustHaveHappenedOnceExactly();
    }
    
    [Theory]
    [MemberData(nameof(VariousCdiStatusCodes))]
    public async Task Handle_WithMessage_AddsExamStatus_CdiEvents(ExamStatusCode status, string eventName)
    {
        var request = CreateRequest(status, eventName);
    
        await CreateSubject().Handle(request, A.Dummy<CancellationToken>());
        
        A.CallTo(() => _mediator.Send(A<CreateStatus>.That.Matches(a =>
                    a.ExamId == request.ExamStatus.ExamId && a.ExamStatusCode == status),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>.That.Matches(c =>
                c.EventType == Constants.Observability.ProviderPay.CdiEvents), false))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Handle_WhenDeterminingWhetherToPublishToKafka_HandlesAllStatusCodes()
    {
        A.CallTo(() => _mediator.Send(A<CreateStatus>._, default)).Returns(A.Fake<CreateStatusResponse>());

        foreach (var statusCode in ExamStatusCode.All)
        {
            var request = CreateRequest(statusCode, nameof(CDIPassedEvent));
            await CreateSubject().Handle(request, default);
        }

        Assert.True(true);
    }

    public static IEnumerable<object[]> VariousCdiStatusCodes()
    {
        yield return new object[] { ExamStatusCode.CdiPassedReceived, nameof(CDIPassedEvent) };
        yield return new object[] { ExamStatusCode.CdiFailedWithPayReceived, nameof(CDIFailedEvent) };
        yield return new object[] { ExamStatusCode.CdiFailedWithoutPayReceived, nameof(CDIFailedEvent) };
    }
}