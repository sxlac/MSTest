using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NsbEventHandlers;
using NServiceBus;
using Signify.Dps.Observability.Library.Services;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Constants;
using Signify.PAD.Svc.Core.Data.Entities;
using Signify.PAD.Svc.Core.Data;
using Signify.PAD.Svc.Core.Events;
using Signify.PAD.Svc.Core.Models;
using Signify.PAD.Svc.Core.Queries;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.EventHandlers.Nsb;

public class ExamStatusHandlerOldTests
{
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly ITransactionSupplier _transactionSupplier = A.Fake<ITransactionSupplier>();
    private readonly ILogger<ExamStatusHandlerOld> _logger = A.Fake<ILogger<ExamStatusHandlerOld>>();
    private readonly IObservabilityService _observabilityService = A.Fake<IObservabilityService>();

    private ExamStatusHandlerOld CreateSubject()
        => new(_logger, _mapper, _mediator, _transactionSupplier, _observabilityService);

    [Fact]
    public async Task Handle_WithMessage_AddsExamStatus_ProviderPayableEventReceived()
    {
        const long evaluationId = 1;
        const int examId = 2;
        var guid = Guid.NewGuid();
        var request = new ProviderPayStatusEvent
        {
            EventId = guid,
            EvaluationId = evaluationId,
            ExamId = examId,
            StatusCode = PADStatusCode.ProviderPayableEventReceived,
            ParentCdiEvent = nameof(CDIPassedEvent)
        };
        var exam = A.Fake<Core.Data.Entities.PAD>();
        A.CallTo(() => _mediator.Send(A<GetPAD>._, A<CancellationToken>._)).Returns(exam);

        await CreateSubject().Handle(request, A.Dummy<IMessageHandlerContext>());

        A.CallTo(() => _mediator.Send(A<GetPAD>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mapper.Map<ProviderPayableEventReceived>(A<Core.Data.Entities.PAD>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreatePadStatus>.That.Matches(a =>
                    a.PadId == request.ExamId && a.StatusCode == PADStatusCode.ProviderPayableEventReceived),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdateOld>.That.Matches(a =>
            a.EventId == guid), A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _observabilityService.AddEvent(Observability.ProviderPay.PayableCdiEvents, A<Dictionary<string, object>>._))
            .MustHaveHappenedOnceExactly();
    }

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
            //ExamId = examId // Not setting
            StatusCode = PADStatusCode.ProviderPayableEventReceived,
            ParentCdiEvent = nameof(CDIPassedEvent)
        };
        A.CallTo(() => _mediator.Send(A<GetPAD>._, A<CancellationToken>._))
            .Returns(new Core.Data.Entities.PAD { PADId = examId });

        await CreateSubject().Handle(request, A.Dummy<IMessageHandlerContext>());

        A.CallTo(() => _mediator.Send(A<GetPAD>.That.Matches(q => q.EvaluationId == evaluationId), A<CancellationToken>._))
            .MustHaveHappenedOnceExactly(); // We did not pass ExamId in the request
        A.CallTo(() => _mediator.Send(
                A<CreatePadStatus>.That.Matches(a => a.PadId == request.ExamId && a.StatusCode == PADStatusCode.ProviderPayableEventReceived),
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
            StatusCode = PADStatusCode.ProviderNonPayableEventReceived,
            ParentCdiEvent = nameof(CDIPassedEvent)
        };
        A.CallTo(() => _mediator.Send(A<GetPAD>._, A<CancellationToken>._))
            .Returns(new Core.Data.Entities.PAD { PADId = examId });

        await CreateSubject().Handle(request, A.Dummy<IMessageHandlerContext>());

        A.CallTo(() => _mediator.Send(A<GetPAD>.That.Matches(q => q.EvaluationId == evaluationId), A<CancellationToken>._))
            .MustHaveHappenedOnceExactly(); // We did not pass ExamId in the request
        A.CallTo(() => _mediator.Send(
                A<CreatePadStatus>.That.Matches(a => a.PadId == request.ExamId && a.StatusCode == PADStatusCode.ProviderNonPayableEventReceived),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }
    
    [Theory]
    [MemberData(nameof(VariousCdiStatusCodes))]
    public async Task Handle_WithMessage_AddsExamStatus_CdiEvents(PADStatusCode status, string eventName)
    {
        const long evaluationId = 1;
        const int examId = 2;
        var guid = Guid.NewGuid();
        var request = new ProviderPayStatusEvent
        {
            EventId = guid,
            EvaluationId = evaluationId,
            ExamId = examId,
            StatusCode = status,
            ParentCdiEvent = eventName
        };
        var exam = A.Fake<Core.Data.Entities.PAD>();
        A.CallTo(() => _mediator.Send(A<GetPAD>._, A<CancellationToken>._)).Returns(exam);

        await CreateSubject().Handle(request, A.Dummy<IMessageHandlerContext>());

        A.CallTo(() => _mediator.Send(A<GetPAD>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreatePadStatus>.That.Matches(a =>
                    a.PadId == request.ExamId && a.StatusCode == status),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdateOld>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _observabilityService.AddEvent(Observability.ProviderPay.CdiEvents, A<Dictionary<string, object>>._))
            .MustHaveHappenedOnceExactly();
    }

    public static IEnumerable<object[]> VariousCdiStatusCodes()
    {
        yield return new object[] { PADStatusCode.CdiPassedReceived, nameof(CDIPassedEvent) };
        yield return new object[] { PADStatusCode.CdiFailedWithPayReceived, nameof(CDIFailedEvent) };
        yield return new object[] { PADStatusCode.CdiFailedWithoutPayReceived, nameof(CDIFailedEvent) };
    }
}