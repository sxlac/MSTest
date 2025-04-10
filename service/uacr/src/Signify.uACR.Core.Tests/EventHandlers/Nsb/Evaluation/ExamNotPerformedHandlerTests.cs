using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NsbEventHandlers;
using NServiceBus;
using NServiceBus.Testing;
using Signify.Dps.Observability.Library.Services;
using Signify.uACR.Core.Commands;
using Signify.uACR.Core.Data;
using Signify.uACR.Core.Data.Entities;
using UacrNsbEvents;
using Xunit;
using NotPerformedReason = Signify.uACR.Core.Models.NotPerformedReason;

namespace Signify.uACR.Core.Tests.EventHandlers.Nsb;

public sealed class ExamNotPerformedHandlerTests : IDisposable, IAsyncDisposable
{
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();
    private readonly MockDbFixture _dbFixture = new();

    public void Dispose()
    {
        _dbFixture.Dispose();
    }

    public ValueTask DisposeAsync()
        => _dbFixture.DisposeAsync();

    private ExamNotPerformedHandler CreateSubject()
        => new(A.Dummy<ILogger<ExamNotPerformedHandler>>(),
            A.Dummy<ITransactionSupplier>(), _mediator, _publishObservability, new FakeApplicationTime());

    [Fact]
    public async Task Handle_WithMessage_AddsExamAndNotPerformedDetails()
    {
        // Arrange
        const long evaluationId = 1;
        const int examId = 2;
        const NotPerformedReason reason = NotPerformedReason.NotInterested;

        var request = new ExamNotPerformedEvent
        {
            Exam = new Exam
            {
                // No ExamId passed into the event; this will be generated by AddExam command
                EvaluationId = evaluationId
            },
            Reason = reason
        };

        var addExamResult = new Exam
        {
            ExamId = examId,
            EvaluationId = evaluationId
        };

        A.CallTo(() => _mediator.Send(A<AddExam>._, A<CancellationToken>._))
            .Returns(addExamResult);

        // Act
        var subject = CreateSubject();

        await subject.Handle(request, A.Dummy<IMessageHandlerContext>());

        // Assert
        A.CallTo(() => _mediator.Send(A<AddExam>.That.Matches(
                cmd => cmd.Exam == request.Exam), A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<AddExamNotPerformed>.That.Matches(
                cmd => cmd.Exam == addExamResult && cmd.NotPerformedReason == reason), A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Handle_WithMessage_SendsExamNotPerformedStatus()
    {
        const long evaluationId = 1;
        const int examId = 2;

        var request = new ExamNotPerformedEvent
        {
            Exam = new Exam
            {
                // No ExamId passed into the event; this will be generated by AddExam command
                EvaluationId = evaluationId,
                EvaluationReceivedDateTime = DateTime.UtcNow
            },
            EventId = Guid.NewGuid()
        };

        var addExamResult = new Exam
        {
            ExamId = examId,
            EvaluationId = evaluationId
        };

        A.CallTo(() => _mediator.Send(A<AddExam>._, A<CancellationToken>._))
            .Returns(addExamResult);

        var context = new TestableMessageHandlerContext();

        var subject = CreateSubject();

        await subject.Handle(request, context);

        Assert.Single(context.SentMessages);
        var message = context.SentMessages.First().Message<ExamStatusEvent>();
        Assert.Equal(ExamStatusCode.ExamNotPerformed, message.StatusCode);
        Assert.Equal(request.EventId, message.EventId);
        Assert.Equal(evaluationId, message.EvaluationId);
        Assert.Equal(examId, message.ExamId);
        Assert.Equal(request.Exam.EvaluationReceivedDateTime, message.StatusDateTime);
    }
}