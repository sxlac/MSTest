using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NsbEventHandlers;
using NServiceBus;
using Signify.CKD.Svc.Core.Commands;
using Signify.CKD.Svc.Core.Constants;
using Signify.CKD.Svc.Core.Data;
using Signify.CKD.Svc.Core.Data.Entities;
using Signify.CKD.Svc.Core.Events;
using Signify.CKD.Svc.Core.Infrastructure.Observability;
using Signify.CKD.Svc.Core.Messages.Status;
using Signify.CKD.Svc.Core.Models;
using Signify.CKD.Svc.Core.Queries;
using Signify.CKD.Svc.Core.Tests.Data;
using Xunit;

namespace Signify.CKD.Svc.Core.Tests.EventHandlers.Nsb;

public class ExamStatusHandlerTests
{
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly FakeTransactionSupplier _transactionSupplier = new();
    private readonly ILogger<ExamStatusHandler> _logger = A.Fake<ILogger<ExamStatusHandler>>();
    private readonly IObservabilityService _observabilityService = A.Fake<IObservabilityService>();

    private ExamStatusHandler CreateSubject()
        => new(_logger, _mapper, _mediator, _transactionSupplier, _observabilityService);

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
            StatusCode = CKDStatusCode.ProviderPayableEventReceived,
            ParentCdiEvent = nameof(CDIPassedEvent)
        };
        A.CallTo(() => _mediator.Send(A<GetCKD>._, A<CancellationToken>._))
            .Returns(new Core.Data.Entities.CKD { CKDId = examId });

        await CreateSubject().Handle(request, A.Dummy<IMessageHandlerContext>());

        A.CallTo(() => _mediator.Send(A<GetCKD>.That.Matches(q => q.EvaluationId == evaluationId), A<CancellationToken>._))
            .MustHaveHappenedOnceExactly(); // We did not pass ExamId in the request
        A.CallTo(() => _mediator.Send(
                A<CreateCKDStatus>.That.Matches(a => a.CKDId == examId && a.StatusCodeId == CKDStatusCode.ProviderPayableEventReceived.CKDStatusCodeId),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _observabilityService.AddEvent(Observability.ProviderPay.PayableCdiEvents, A<Dictionary<string, object>>._))
            .MustHaveHappenedOnceExactly();

        _transactionSupplier.AssertCommit();
    }

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
            StatusCode = CKDStatusCode.ProviderPayableEventReceived,
            ParentCdiEvent = nameof(CDIPassedEvent)
        };
        A.CallTo(() => _mediator.Send(A<GetCKD>._, A<CancellationToken>._)).Returns(new Core.Data.Entities.CKD { CKDId = examId });

        await CreateSubject().Handle(request, A.Dummy<IMessageHandlerContext>());

        A.CallTo(() => _mediator.Send(A<GetCKD>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mapper.Map<ProviderPayableEventReceived>(A<Core.Data.Entities.CKD>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreateCKDStatus>.That.Matches(a =>
                    a.CKDId == examId && a.StatusCodeId == CKDStatusCode.ProviderPayableEventReceived.CKDStatusCodeId),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _observabilityService.AddEvent(Observability.ProviderPay.PayableCdiEvents, A<Dictionary<string, object>>._))
            .MustHaveHappenedOnceExactly();

        _transactionSupplier.AssertCommit();
    }

    [Fact]
    public async Task Handle_WithMessage_AddsExamStatus_ProviderPayRequestSent()
    {
        const long evaluationId = 1;
        const int examId = 2;
        var guid = Guid.NewGuid();
        var request = new ProviderPayStatusEvent
        {
            EventId = guid,
            EvaluationId = evaluationId,
            StatusCode = CKDStatusCode.ProviderPayRequestSent,
            ParentCdiEvent = nameof(CDIPassedEvent)
        };
        A.CallTo(() => _mediator.Send(A<GetCKD>._, A<CancellationToken>._)).Returns(new Core.Data.Entities.CKD { CKDId = examId });

        await CreateSubject().Handle(request, A.Dummy<IMessageHandlerContext>());

        A.CallTo(() => _mediator.Send(A<GetCKD>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mapper.Map<ProviderPayRequestSent>(A<Core.Data.Entities.CKD>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreateCKDStatus>.That.Matches(a =>
                    a.CKDId == examId && a.StatusCodeId == CKDStatusCode.ProviderPayRequestSent.CKDStatusCodeId),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();

        _transactionSupplier.AssertCommit();
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
            StatusCode = CKDStatusCode.ProviderNonPayableEventReceived,
            ParentCdiEvent = nameof(CDIPassedEvent)
        };
        A.CallTo(() => _mediator.Send(A<GetCKD>._, A<CancellationToken>._))
            .Returns(new Core.Data.Entities.CKD { CKDId = examId });

        await CreateSubject().Handle(request, A.Dummy<IMessageHandlerContext>());

        A.CallTo(() => _mediator.Send(A<GetCKD>.That.Matches(q => q.EvaluationId == evaluationId), A<CancellationToken>._))
            .MustHaveHappenedOnceExactly(); // We did not pass ExamId in the request
        A.CallTo(() => _mediator.Send(
                A<CreateCKDStatus>.That.Matches(a => a.CKDId == examId && a.StatusCodeId == CKDStatusCode.ProviderNonPayableEventReceived.CKDStatusCodeId),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _observabilityService.AddEvent(Observability.ProviderPay.NonPayableCdiEvents, A<Dictionary<string, object>>._))
            .MustHaveHappenedOnceExactly();
    }
    
    [Theory]
    [MemberData(nameof(VariousCdiStatusCodes))]
    public async Task Handle_WithMessage_AddsExamStatus_CdiEvents(CKDStatusCode status, string eventName)
    {
        const long evaluationId = 1;
        var guid = Guid.NewGuid();
        var request = new ProviderPayStatusEvent
        {
            EventId = guid,
            EvaluationId = evaluationId,
            StatusCode = status,
            ParentCdiEvent = eventName
        };
        A.CallTo(() => _mediator.Send(A<GetCKD>._, A<CancellationToken>._))
            .Returns(new Core.Data.Entities.CKD { EvaluationId = evaluationId });

        await CreateSubject().Handle(request, A.Dummy<IMessageHandlerContext>());

        A.CallTo(() => _mediator.Send(A<GetCKD>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreateCKDStatus>.That.Matches(a => a.StatusCodeId == status.CKDStatusCodeId),
            A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _observabilityService.AddEvent(Observability.ProviderPay.CdiEvents, A<Dictionary<string, object>>._))
            .MustHaveHappenedOnceExactly();

        _transactionSupplier.AssertCommit();
    }

    public static IEnumerable<object[]> VariousCdiStatusCodes()
    {
        yield return new object[] { CKDStatusCode.CdiPassedReceived, nameof(CDIPassedEvent) };
        yield return new object[] { CKDStatusCode.CdiFailedWithPayReceived, nameof(CDIFailedEvent) };
        yield return new object[] { CKDStatusCode.CdiFailedWithoutPayReceived, nameof(CDIFailedEvent) };
    }
}