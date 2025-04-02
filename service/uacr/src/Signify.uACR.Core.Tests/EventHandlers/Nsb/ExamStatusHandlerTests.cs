using AutoMapper;
using UacrNsbEvents;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NsbEventHandlers;
using NServiceBus;
using Signify.uACR.Core.Commands;
using Signify.uACR.Core.Data.Entities;
using Signify.uACR.Core.Data;
using Signify.uACR.Core.Events.Status;
using Signify.uACR.Core.Queries;
using System.Threading.Tasks;
using System.Threading;
using System;
using Signify.uACR.Core.Constants;
using Xunit;

namespace Signify.uACR.Core.Tests.EventHandlers.Nsb;

public class ExamStatusHandlerTests
{
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly ITransactionSupplier _transactionSupplier = A.Fake<ITransactionSupplier>();
    private readonly ILogger<ExamStatusHandler> _logger = A.Fake<ILogger<ExamStatusHandler>>();
    private readonly IAgent _nrAgent = A.Fake<IAgent>();

    private ExamStatusHandler CreateSubject()
        => new(_logger, _mapper, _mediator, _transactionSupplier, _nrAgent);

    [Fact]
    public async Task Handle_WithMessage_AddsExamStatus()
    {
        const long evaluationId = 1;
        const int examId = 2;

        var request = new ExamStatusEvent
        {
            EventId = Guid.NewGuid(),
            EvaluationId = evaluationId,
            ExamId = examId,
            StatusCode = ExamStatusCode.ExamPerformed,
            RcmProductCode = ProductCodes.uACR_RcmBilling
        };

        var status = new ExamStatus
        {
            ExamId = examId
        };

        A.CallTo(() => _mapper.Map<ExamStatus>(A<ExamStatusEvent>._))
            .Returns(status);

        await CreateSubject().Handle(request, A.Dummy<IMessageHandlerContext>());

        A.CallTo(() => _mediator.Send(A<QueryExamByEvaluation>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _mediator.Send(
                A<AddExamStatus>.That.Matches(a =>
                    a.EventId == request.EventId && a.EvaluationId == evaluationId && a.Status == status),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Handle_WithMessage_AddsExamStatus_SetExamId_IfNotSet()
    {
        const long evaluationId = 1;
        const int examId = 2;

        var request = new ExamStatusEvent
        {
            EventId = Guid.NewGuid(),
            EvaluationId = evaluationId,
            StatusCode = ExamStatusCode.ExamPerformed,
            RcmProductCode = ProductCodes.uACR_RcmBilling
            //ExamId = ExamId // Not setting
        };

        var status = new ExamStatus();

        A.CallTo(() => _mapper.Map<ExamStatus>(A<ExamStatusEvent>._))
            .Returns(status);

        A.CallTo(() => _mediator.Send(A<QueryExamByEvaluation>._, A<CancellationToken>._))
            .Returns(new Exam { ExamId = examId });

        await CreateSubject().Handle(request, A.Dummy<IMessageHandlerContext>());

        A.CallTo(() =>
                _mediator.Send(A<QueryExamByEvaluation>.That.Matches(q => q.EvaluationId == evaluationId), A<CancellationToken>._))
            .MustHaveHappenedOnceExactly(); // We did not pass ExamId in the request

        A.CallTo(() => _mediator.Send(
                A<AddExamStatus>.That.Matches(a =>
                    a.EventId == request.EventId && a.EvaluationId == evaluationId && a.Status == status),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        Assert.Equal(examId, status.ExamId); // Ensure this was set after querying for it
    }

    [Fact]
    public async Task Handle_WriteToDatabase_And_Publish_Performed()
    {
        const long evaluationId = 1;
        const int examId = 2;

        var request = new ExamStatusEvent
        {
            EventId = Guid.NewGuid(),
            EvaluationId = evaluationId,
            ExamId = examId,
            StatusCode = ExamStatusCode.ExamPerformed,
            RcmProductCode = ProductCodes.uACR_RcmBilling
        };

        var status = new ExamStatus
        {
            ExamId = examId
        };

        A.CallTo(() => _mapper.Map<ExamStatus>(A<ExamStatusEvent>._))
            .Returns(status);

        await CreateSubject().Handle(request, A.Dummy<IMessageHandlerContext>());

        A.CallTo(() => _mediator.Send(A<QueryExamByEvaluation>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _mediator.Send(
                A<AddExamStatus>.That.Matches(a =>
                    a.EventId == request.EventId && a.EvaluationId == evaluationId && a.Status == status),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>.That.Matches(a =>
                a.Status is Performed), A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Handle_WriteToDatabase_And_Publish_NotPerformed()
    {
        const long evaluationId = 1;
        const int examId = 2;

        var request = new ExamStatusEvent
        {
            EventId = Guid.NewGuid(),
            EvaluationId = evaluationId,
            ExamId = examId,
            StatusCode = ExamStatusCode.ExamNotPerformed,
            RcmProductCode = ProductCodes.uACR_RcmBilling
        };

        var status = new ExamStatus
        {
            ExamId = examId
        };

        var examNotPerformedEntity = new ExamNotPerformed
        {
            NotPerformedReason = NotPerformedReason.NotInterested
        };

        A.CallTo(() => _mapper.Map<ExamStatus>(A<ExamStatusEvent>._))
            .Returns(status);
        A.CallTo(() => _mediator.Send(A<QueryExamNotPerformed>._, A<CancellationToken>._))
            .Returns(examNotPerformedEntity);

        await CreateSubject().Handle(request, A.Dummy<IMessageHandlerContext>());

        A.CallTo(() => _mediator.Send(A<QueryExamByEvaluation>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _mediator.Send(
                A<AddExamStatus>.That.Matches(a =>
                    a.EventId == request.EventId && a.EvaluationId == evaluationId && a.Status == status),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>.That.Matches(a =>
                a.Status is NotPerformed), A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }
}