using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using EgfrNsbEvents;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NsbEventHandlers;
using NServiceBus;
using Signify.Dps.Observability.Library.Services;
using Signify.eGFR.Core.Commands;
using Signify.eGFR.Core.Data.Entities;
using Signify.eGFR.Core.Models;
using Signify.eGFR.Core.Queries;
using Xunit;

namespace Signify.eGFR.Core.Tests.EventHandlers.Nsb.Evaluation;

public sealed class EvalReceivedHandlerTests : IDisposable, IAsyncDisposable
{
    private readonly IMediator _fakeMediator = A.Fake<IMediator>();
    private readonly IMessageHandlerContext _fakeContext = A.Fake<IMessageHandlerContext>();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();
    private readonly FakeApplicationTime _applicationTime = new();
    private readonly FakeTransactionSupplier _transactionSupplier = A.Fake<FakeTransactionSupplier>();
    private readonly MockDbFixture _dbFixture = new();

    public void Dispose()
    {
        _dbFixture.Dispose();
    }

    public ValueTask DisposeAsync() => _dbFixture.DisposeAsync();

    private EvalReceivedHandler CreateSubject() => new(A.Dummy<ILogger<EvalReceivedHandler>>(), _fakeMediator,
        _transactionSupplier, _publishObservability, _applicationTime);

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

    [Fact]
    public async Task Handle_WithPerformedExam_WhereEvaluationIsNotInDb_SendsLocalExamPerformedEvent()
    {
        const int evaluationId = 1;

        var evalReceived = new EvalReceived
        {
            EvaluationId = evaluationId,
            Id = Guid.NewGuid()
        };

        var examModel = new ExamModel(evaluationId);

        // Setup calls that need to return the objects defined above
        SetupMediator<QueryExam, Exam>(null, _ => true); // not yet in db
        SetupMediator<GenerateExamModel, ExamModel>(examModel, _ => true);

        var subject = CreateSubject();

        await subject.Handle(evalReceived, _fakeContext);
            
        // Verify sent once
        VerifyWasSentToMediator<QueryExam, Exam>(
            cmd => cmd.EvaluationId == evaluationId);
        VerifyWasSentToMediator<GenerateExamModel, ExamModel>(
            cmd => cmd.EvaluationId == evaluationId);

        // Verify never sent
        VerifyWasSentToMediator<AddExam, Exam>(_ => true, 0);
        VerifyWasSentToMediator<UpdateExam, Exam>(_ => true, 0);
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
        SetupMediator<QueryExam, Exam>(new Exam(), _ => true); // exists in db
        SetupMediator<UpdateExam, Exam>(new Exam(), _ => true);

        var subject = CreateSubject();

        await subject.Handle(evalReceived, A.Dummy<IMessageHandlerContext>());
            
        // Verify sent once
        VerifyWasSentToMediator<QueryExam, Exam>(
            cmd => cmd.EvaluationId == evaluationId);
        VerifyWasSentToMediator<UpdateExam, Exam>(
            cmd => cmd.EventData == evalReceived);

        // Verify never sent
        VerifyWasSentToMediator<GenerateExamModel, ExamModel>(_ => true, 0);
        VerifyWasSentToMediator<AggregateExamDetails, Exam>(_ => true, 0);
    }       
}