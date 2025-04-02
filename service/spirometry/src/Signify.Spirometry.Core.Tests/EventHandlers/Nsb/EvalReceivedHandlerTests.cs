using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NsbEventHandlers;
using NServiceBus;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using Signify.Spirometry.Core.Commands;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Models;
using Signify.Spirometry.Core.Queries;
using SpiroNsbEvents;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;
using NotPerformedReason = Signify.Spirometry.Core.Models.NotPerformedReason;

namespace Signify.Spirometry.Core.Tests.EventHandlers.Nsb;

public sealed class EvalReceivedHandlerTests : IDisposable, IAsyncDisposable
{
    private readonly IMediator _fakeMediator = A.Fake<IMediator>();
    private readonly IMessageHandlerContext _fakeContext = A.Fake<IMessageHandlerContext>();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();
    private readonly MockDbFixture _dbFixture = new();

    public void Dispose()
    {
        _dbFixture.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _dbFixture.DisposeAsync();
    }

    private EvalReceivedHandler CreateSubject()
    {
        return new EvalReceivedHandler(A.Dummy<ILogger<EvalReceivedHandler>>(),
            _fakeMediator, _publishObservability);
    }

    private void SetupMediator<TCommand, TResponse>(TResponse response, Expression<Func<TCommand, bool>> predicate)
        where TCommand: class, IRequest<TResponse>
    {
        A.CallTo(() => _fakeMediator.Send(A<TCommand>.That.Matches(predicate), A<CancellationToken>._))
            .Returns(response);
    }

    private void VerifyWasSentToMediator<TCommand, TResponse>(Expression<Func<TCommand, bool>> predicate, int numberOfTimes = 1)
        where TCommand: class, IRequest<TResponse>
    {
        A.CallTo(() => _fakeMediator.Send(A<TCommand>.That.Matches(predicate), A<CancellationToken>._))
            .MustHaveHappened(numberOfTimes, Times.Exactly);
    }

    private void VerifySentLocal<TEvent>(Expression<Func<TEvent, bool>> predicate, int numberOfTimes = 1)
    {
        // We can't verify a call directly to IPipelineContext.SendLocal<T>, as that is an extension method.
        // Instead, this is verifying a call to the inner IPipelineContext.Send<T>(Action<T> messageConstructor, SendOptions options) method call.
        A.CallTo(() => _fakeContext.Send(A<TEvent>.That.Matches(predicate), A<SendOptions>._))
            .MustHaveHappened(numberOfTimes, Times.Exactly);
    }

    [Fact]
    public async Task Handle_WithPerformedExam_WhereEvaluationIsNotInDb_SendsLocalExamPerformedEvent()
    {
        const int evaluationId = 1;
        const int spirometryExamId = 2;
        const int formVersionId = 3;

        var evalReceived = new EvalReceived
        {
            EvaluationId = evaluationId,
            Id = Guid.NewGuid()
        };

        var examModel = new PerformedExamModel(evaluationId, new RawExamResult())
        {
            FormVersionId = formVersionId
        };

        var examDto = new SpirometryExam
        {
            SpirometryExamId = spirometryExamId,
            EvaluationId = evaluationId
        };

        // Setup calls that need to return the objects defined above
        SetupMediator<QuerySpirometryExam, SpirometryExam>(null, _ => true); // not yet in db
        SetupMediator<GenerateExamModel, ExamModel>(examModel, _ => true);
        SetupMediator<AggregateSpirometryExamDetails, SpirometryExam>(examDto, _ => true);

        var subject = CreateSubject();

        await subject.Handle(evalReceived, _fakeContext);

        // Verify sent once
        VerifyWasSentToMediator<QuerySpirometryExam, SpirometryExam>(
            cmd => cmd.EvaluationId == evaluationId);
        VerifyWasSentToMediator<GenerateExamModel, ExamModel>(
            cmd => cmd.EvaluationId == evaluationId);
        VerifyWasSentToMediator<AggregateSpirometryExamDetails, SpirometryExam>(
            cmd => cmd.EventData.EvaluationId == evaluationId);

        VerifySentLocal<ExamPerformedEvent>(e =>
            e.Exam == examDto &&
            e.Exam.FormVersionId == formVersionId &&
            e.Result == examModel.ExamResult &&
            e.EventId == evalReceived.Id);

        // Verify never sent
        VerifyWasSentToMediator<AddExam, SpirometryExam>(_ => true, 0);
        VerifyWasSentToMediator<UpdateExam, SpirometryExam>(_ => true, 0);
        VerifySentLocal<ExamNotPerformedEvent>(_ => true, 0);
            
        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>._, true)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Handle_WithPerformedExam_WhereEvaluationExistsInDb_SendsUpdateExamCommand()
    {
        const int evaluationId = 1;

        var evalReceived = new EvalReceived
        {
            EvaluationId = evaluationId
        };

        // Setup calls that need to return the objects defined above
        SetupMediator<QuerySpirometryExam, SpirometryExam>(new SpirometryExam(), _ => true); // exists in db
        SetupMediator<UpdateExam, SpirometryExam>(new SpirometryExam(), _ => true);

        var subject = CreateSubject();

        await subject.Handle(evalReceived, A.Dummy<IMessageHandlerContext>());

        // Verify sent once
        VerifyWasSentToMediator<QuerySpirometryExam, SpirometryExam>(
            cmd => cmd.EvaluationId == evaluationId);
        VerifyWasSentToMediator<UpdateExam, SpirometryExam>(
            cmd => cmd.EventData == evalReceived);

        // Verify never sent
        VerifyWasSentToMediator<GenerateExamModel, ExamModel>(_ => true, 0);
        VerifyWasSentToMediator<AggregateSpirometryExamDetails, SpirometryExam>(_ => true, 0);
            
        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>._, true)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Handle_WithNotPerformedExam_SendsLocalExamNotPerformedEvent()
    {
        const int evaluationId = 1;
        const int spirometryExamId = 2;
        const int formVersionId = 3;
        const NotPerformedReason reason = NotPerformedReason.NotInterested;
        const string notes = nameof(notes);

        var evalReceived = new EvalReceived
        {
            EvaluationId = evaluationId,
            Id = Guid.NewGuid()
        };

        var examModel = new NotPerformedExamModel(evaluationId, new NotPerformedInfo(reason, notes))
        {
            FormVersionId = formVersionId
        };

        var examDto = new SpirometryExam
        {
            SpirometryExamId = spirometryExamId,
            EvaluationId = evaluationId
        };

        // Setup calls that need to return the objects defined above
        SetupMediator<QuerySpirometryExam, SpirometryExam>(null, _ => true); // not yet in db
        SetupMediator<GenerateExamModel, ExamModel>(examModel, _ => true);
        SetupMediator<AggregateSpirometryExamDetails, SpirometryExam>(examDto, _ => true);

        var subject = CreateSubject();

        await subject.Handle(evalReceived, _fakeContext);

        // Verify sent once
        VerifyWasSentToMediator<QuerySpirometryExam, SpirometryExam>(
            cmd => cmd.EvaluationId == evaluationId);
        VerifyWasSentToMediator<GenerateExamModel, ExamModel>(
            cmd => cmd.EvaluationId == evaluationId);
        VerifyWasSentToMediator<AggregateSpirometryExamDetails, SpirometryExam>(
            cmd => cmd.EventData.EvaluationId == evaluationId);

        VerifySentLocal<ExamNotPerformedEvent>(e =>
            e.Exam == examDto &&
            e.Exam.FormVersionId == formVersionId &&
            e.Info.Reason == reason &&
            e.Info.Notes == notes &&
            e.EventId == evalReceived.Id);

        // Verify never sent
        VerifyWasSentToMediator<AddExam, SpirometryExam>(_ => true, 0);
        VerifyWasSentToMediator<UpdateExam, SpirometryExam>(_ => true, 0);
        VerifySentLocal<ExamPerformedEvent>(_ => true, 0);
    }
}