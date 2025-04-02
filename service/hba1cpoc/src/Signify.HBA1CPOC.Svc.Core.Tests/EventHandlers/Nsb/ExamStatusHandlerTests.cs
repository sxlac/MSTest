using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Signify.HBA1CPOC.Messages.Events.Akka;
using Signify.HBA1CPOC.Messages.Events.Status;
using Signify.HBA1CPOC.Svc.Core.Commands;
using Signify.HBA1CPOC.Svc.Core.Data.Entities;
using Signify.HBA1CPOC.Svc.Core.Data;
using Signify.HBA1CPOC.Svc.Core.EventHandlers;
using Signify.HBA1CPOC.Svc.Core.Infrastructure.Observability;
using Signify.HBA1CPOC.Svc.Core.Queries;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.HBA1CPOC.Svc.Core.Tests.EventHandlers.Nsb;

public class ExamStatusHandlerTests
{
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly ITransactionSupplier _transactionSupplier = A.Fake<ITransactionSupplier>();
    private readonly ILogger<ExamStatusHandler> _logger = A.Fake<ILogger<ExamStatusHandler>>();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();
    private ExamStatusHandler CreateSubject()
        => new(_logger, _mapper, _mediator, _transactionSupplier, _publishObservability);
    
    [Fact]
    public async Task Handle_WithMessage_AddsExamStatus_SetExamId_IfNotSet()
    {
        const long evaluationId = 1;
        const int examId = 2;
        var guid = Guid.NewGuid();
        var request = new ProviderPayStatusEvent
        {
            EventId = guid,
            EvaluationId = evaluationId,
            StatusCode = HBA1CPOCStatusCode.ProviderPayableEventReceived.HBA1CPOCStatusCodeId,
            ParentCdiEvent = nameof(CDIPassedEvent)
        };
        A.CallTo(() => _mediator.Send(A<GetHBA1CPOC>._, A<CancellationToken>._))
            .Returns(new Core.Data.Entities.HBA1CPOC() { HBA1CPOCId = examId });
        await CreateSubject().Handle(request, A.Dummy<IMessageHandlerContext>());
        A.CallTo(() => _mediator.Send(A<GetHBA1CPOC>.That.Matches(q => q.EvaluationId == evaluationId), A<CancellationToken>._))
            .MustHaveHappenedOnceExactly(); // We did not pass ExamId in the request
        A.CallTo(() => _mediator.Send(
                A<CreateHBA1CPOCStatus>.That.Matches(a => a.HBA1CPOCId == examId && a.StatusCodeId == HBA1CPOCStatusCode.ProviderPayableEventReceived.HBA1CPOCStatusCodeId),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }
    
    [Fact]
    public async Task Handle_ExamStatus_With_StatusCode_ProviderNonPayableEventReceived()
    {
        const long evaluationId = 1;
        const int examId = 2;
        var guid = Guid.NewGuid();
        var request = new ProviderPayStatusEvent
        {
            EventId = guid,
            EvaluationId = evaluationId,
            StatusCode = HBA1CPOCStatusCode.ProviderNonPayableEventReceived.HBA1CPOCStatusCodeId,
            ParentCdiEvent = nameof(CDIPassedEvent)
        };
        A.CallTo(() => _mediator.Send(A<GetHBA1CPOC>._, A<CancellationToken>._))
            .Returns(new Core.Data.Entities.HBA1CPOC {HBA1CPOCId = examId });

        await CreateSubject().Handle(request, A.Dummy<IMessageHandlerContext>());

        A.CallTo(() => _mediator.Send(A<GetHBA1CPOC>.That.Matches(q => q.EvaluationId == evaluationId), A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(
                A<CreateHBA1CPOCStatus>.That.Matches(a => a.HBA1CPOCId == examId && a.StatusCodeId == HBA1CPOCStatusCode.ProviderNonPayableEventReceived.HBA1CPOCStatusCodeId),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }
    
    [Theory]
    [MemberData(nameof(VariousCdiStatusCodes))]
    public async Task Handle_WithMessage_AddsExamStatus_CdiEvents(HBA1CPOCStatusCode status, string eventName)
    {
        const long evaluationId = 1;
        var guid = Guid.NewGuid();
        var request = new ProviderPayStatusEvent
        {
            EventId = guid,
            EvaluationId = evaluationId,
            StatusCode = status.HBA1CPOCStatusCodeId,
            ParentCdiEvent = eventName
        };
        var exam = A.Fake<Core.Data.Entities.HBA1CPOC>();
        A.CallTo(() => _mediator.Send(A<GetHBA1CPOC>._, A<CancellationToken>._)).Returns(exam);

        await CreateSubject().Handle(request, A.Dummy<IMessageHandlerContext>());

        A.CallTo(() => _mediator.Send(A<GetHBA1CPOC>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreateHBA1CPOCStatus>.That.Matches(a =>
            a.HBA1CPOCId == exam.HBA1CPOCId && a.StatusCodeId == status.HBA1CPOCStatusCodeId ), A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>._, A<CancellationToken>._)).MustNotHaveHappened();
        
        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>.That.Matches(c =>
                c.EventType == Constants.Observability.ProviderPay.CdiEvents), true))
            .MustHaveHappenedOnceExactly();
    }

    public static IEnumerable<object[]> VariousCdiStatusCodes()
    {
        yield return [HBA1CPOCStatusCode.CdiPassedReceived, nameof(CDIPassedEvent)];
        yield return [HBA1CPOCStatusCode.CdiFailedWithPayReceived, nameof(CDIFailedEvent)];
        yield return [HBA1CPOCStatusCode.CdiFailedWithoutPayReceived, nameof(CDIFailedEvent)];
    }
}
