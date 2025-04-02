using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NsbEventHandlers;
using NServiceBus.Testing;
using Signify.FOBT.Svc.Core.ApiClient;
using Signify.FOBT.Svc.Core.Commands;
using Signify.FOBT.Svc.Core.Data.Entities;
using Signify.FOBT.Svc.Core.Events;
using Signify.FOBT.Svc.Core.Infrastructure.Observability;
using Signify.FOBT.Svc.Core.Models;
using Signify.FOBT.Svc.Core.Queries;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.EventHandlers.Nsb;

public class CdiFailedEventHandlerTests
{
    private readonly FakeTransactionSupplier _transactionSupplier = new();
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();
    private readonly IEvaluationApi _evaluationApi = A.Fake<IEvaluationApi>();
    private readonly FakeApplicationTime _applicationTime = new();

    private CdiFailedEventHandler CreateSubject()
        => new(A.Dummy<ILogger<CdiFailedEventHandler>>(), _transactionSupplier, _mediator, _mapper, _publishObservability, _evaluationApi, _applicationTime);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_WhenNotPerformed_DoesNothing(bool payProvider)
    {
        // Arrange
        var request = new CDIFailedEvent
        {
            DateTime = DateTimeOffset.UtcNow,
            PayProvider = payProvider
        };
        A.CallTo(() => _mediator.Send(A<GetFOBT>._, A<CancellationToken>._))
            .Returns(new Core.Data.Entities.FOBT { });
        A.CallTo(() => _mediator.Send(A<QueryExamStatuses>._, A<CancellationToken>._))
            .Returns([
                new FOBTStatus
                {
                    FOBTStatusCodeId = FOBTStatusCode.FOBTNotPerformed.FOBTStatusCodeId
                }
            ]);
        var context = new TestableMessageHandlerContext();

        // Act
        await CreateSubject().Handle(request, context);

        // Assert
        _transactionSupplier.AssertRollback();
        A.CallTo(() => _mediator.Send(A<ExamStatusEvent>._, A<CancellationToken>._)).MustNotHaveHappened();
        Assert.Empty(context.SentMessages);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_WhenNo_PerformedAndNotPerformed_ThrowsException(bool payProvider)
    {
        // Arrange
        var request = new CDIFailedEvent
        {
            DateTime = DateTimeOffset.UtcNow,
            PayProvider = payProvider
        };
        A.CallTo(() => _mediator.Send(A<GetFOBT>._, A<CancellationToken>._))
            .Returns(new Core.Data.Entities.FOBT { });
        A.CallTo(() => _mediator.Send(A<QueryExamStatuses>._, A<CancellationToken>._))
            .Returns([
                new FOBTStatus
                {
                    FOBTStatusCodeId = FOBTStatusCode.InventoryUpdateFail.FOBTStatusCodeId
                }
            ]);
        var context = new TestableMessageHandlerContext();

        // Act and Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await CreateSubject().Handle(request, context));
        _transactionSupplier.AssertRollback();
        Assert.Empty(context.SentMessages);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public async Task Handle_WhenPerformed_And_SatisfiesRules_PublishesEvents(bool payProvider, int count)
    {
        // Arrange
        int? evaluationId = 1;
        var request = new CDIFailedEvent
        {
            EvaluationId = evaluationId!.Value,
            DateTime = DateTimeOffset.UtcNow,
            PayProvider = payProvider
        };
        A.CallTo(() => _mediator.Send(A<GetFOBT>._, A<CancellationToken>._))
            .Returns(new Core.Data.Entities.FOBT { EvaluationId = evaluationId });
        A.CallTo(() => _mediator.Send(A<QueryExamStatuses>._, A<CancellationToken>._))
            .Returns([
                new FOBTStatus
                {
                    FOBTStatusCodeId = FOBTStatusCode.FOBTPerformed.FOBTStatusCodeId
                },

                new FOBTStatus
                {
                    FOBTStatusCodeId = FOBTStatusCode.InvalidLabResultsReceived.FOBTStatusCodeId
                }
            ]);

        var context = new TestableMessageHandlerContext();

        // Act
        await CreateSubject().Handle(request, context);

        // Assert
        _transactionSupplier.AssertCommit();
        A.CallTo(() => _publishObservability.Commit()).MustHaveHappenedOnceExactly();
        Assert.Equal(count, context.SentMessages.Length);
        var statusCode = payProvider ? FOBTStatusCode.CdiFailedWithPayReceived : FOBTStatusCode.CdiFailedWithoutPayReceived;
        A.CallTo(() => _mediator.Send(A<UpdateExamStatus>.That.Matches(s =>
                s.ExamStatus.StatusCode.FOBTStatusCodeId == statusCode.FOBTStatusCodeId &&
                s.ExamStatus.EvaluationId == evaluationId &&
                s.ExamStatus.StatusDateTime == request.DateTime &&
                ((ProviderPayStatusEvent)s.ExamStatus).ParentCdiEvent == nameof(CDIFailedEvent)),
            A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        if (!payProvider)
        {
            A.CallTo(() => _mediator.Send(A<UpdateExamStatus>.That.Matches(s =>
                    s.ExamStatus.StatusCode.FOBTStatusCodeId == FOBTStatusCode.ProviderNonPayableEventReceived.FOBTStatusCodeId &&
                    s.ExamStatus.EvaluationId == evaluationId &&
                    s.ExamStatus.StatusDateTime == request.DateTime &&
                    ((ProviderPayStatusEvent)s.ExamStatus).ParentCdiEvent == nameof(CDIFailedEvent) &&
                    ((ProviderPayStatusEvent)s.ExamStatus).Reason != null),
                A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        }
    }
}