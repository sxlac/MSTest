using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NsbEventHandlers;
using NServiceBus;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using Signify.uACR.Core.Commands;
using Signify.uACR.Core.Constants;
using Signify.uACR.Core.Data.Entities;
using Signify.uACR.Core.Models;
using Signify.uACR.Core.Queries;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Threading;
using System;
using Signify.uACR.Core.Data;
using UacrNsbEvents;
using Xunit;
using NotPerformedReason = Signify.uACR.Core.Models.NotPerformedReason;

namespace Signify.uACR.Core.Tests.EventHandlers.Nsb;

public sealed class EvalReceivedHandlerTests : IDisposable, IAsyncDisposable
{
    private readonly IMediator _fakeMediator = A.Fake<IMediator>();
    private readonly IMessageHandlerContext _fakeContext = A.Fake<IMessageHandlerContext>();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();
    private readonly ITransactionSupplier _transactionSupplier = A.Fake<ITransactionSupplier>();
    private readonly MockDbFixture _dbFixture = new();
    private readonly FakeApplicationTime _applicationTime = new();

    public void Dispose()
    {
        _dbFixture.Dispose();
    }

    public ValueTask DisposeAsync() => _dbFixture.DisposeAsync();

    private EvalReceivedHandler CreateSubject()
    {
        return new EvalReceivedHandler(A.Dummy<ILogger<EvalReceivedHandler>>(),
            _fakeMediator, _transactionSupplier, _publishObservability, _applicationTime);
    }

    [Fact]
    public async Task Handle_EvalReceived_UpdateExam()
    {
        var request = new EvalReceived
        {
            Id = Guid.Empty,
            ApplicationId = "uACR",
            EvaluationId = 1,
            AppointmentId = 1,
            ProviderId = 1,
            MemberId = 1,
            MemberPlanId = 1,
            ClientId = 1,
            CreatedDateTime = DateTimeOffset.Now,
            ReceivedDateTime = DateTimeOffset.Now,
            DateOfService = DateTimeOffset.Now,
            ReceivedByUacrProcessManagerDateTime = DateTimeOffset.Now
        };
        
        await CreateSubject().Handle(request, _fakeContext);
        
        A.CallTo(() => _fakeMediator.Send(A<UpdateExam>._, A<CancellationToken>._)).MustHaveHappened();
    }

    [Fact]
    public async Task Handle_EvalReceived_NewExamPerformed()
    {
        var request = new EvalReceived
        {
            Id = Guid.Empty,
            ApplicationId = "uACR",
            EvaluationId = 1000000,
            AppointmentId = 1,
            ProviderId = 1,
            MemberId = 1,
            MemberPlanId = 1,
            ClientId = 1,
            CreatedDateTime = DateTimeOffset.Now,
            ReceivedDateTime = DateTimeOffset.Now,
            DateOfService = DateTimeOffset.Now,
            ReceivedByUacrProcessManagerDateTime = DateTimeOffset.Now
        };
        
        A.CallTo(() => _fakeMediator.Send(A<QueryExamByEvaluation>._, A<CancellationToken>._))
            .Returns((Exam)null);
        A.CallTo(() => _fakeMediator.Send(A<GenerateExamModel>._, A<CancellationToken>._))
            .Returns(new ExamModel(request.EvaluationId)
            {
                FormVersionId = 1,
                Notes = "Notes123",
            });
        
        await CreateSubject().Handle(request, _fakeContext);
        
        A.CallTo(() => _fakeMediator.Send(A<UpdateExam>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>.That.Matches(e => e.EventType == Observability.Evaluation.EvaluationReceivedEvent), true)).MustHaveHappened();
    }
    
    [Fact]
    public async Task Handle_EvalReceived_NewExamNotPerformed()
    {
        var request = new EvalReceived
        {
            Id = Guid.Empty,
            ApplicationId = "uACR",
            EvaluationId = 1000000,
            AppointmentId = 1,
            ProviderId = 1,
            MemberId = 1,
            MemberPlanId = 1,
            ClientId = 1,
            CreatedDateTime = DateTimeOffset.Now,
            ReceivedDateTime = DateTimeOffset.Now,
            DateOfService = DateTimeOffset.Now,
            ReceivedByUacrProcessManagerDateTime = DateTimeOffset.Now
        };
        
        A.CallTo(() => _fakeMediator.Send(A<QueryExamByEvaluation>._, A<CancellationToken>._))
            .Returns((Exam)null);
        A.CallTo(() => _fakeMediator.Send(A<GenerateExamModel>._, A<CancellationToken>._))
            .Returns(new ExamModel(request.EvaluationId, NotPerformedReason.NoSuppliesOrEquipment, "No equipment")
            {
                FormVersionId = 1,
                Notes = "Notes123",
            });
        
        await CreateSubject().Handle(request, _fakeContext);
        
        A.CallTo(() => _fakeMediator.Send(A<UpdateExam>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>.That.Matches(e => e.EventType == Observability.Evaluation.EvaluationReceivedEvent), true)).MustHaveHappened();
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
}