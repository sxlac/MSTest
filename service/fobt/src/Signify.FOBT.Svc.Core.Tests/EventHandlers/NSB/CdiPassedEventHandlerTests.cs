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
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.EventHandlers.Nsb;

public class CdiPassedEventHandlerTests
{
    private readonly FakeTransactionSupplier _transactionSupplier = new();
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();
    private readonly IEvaluationApi _evaluationApi = A.Fake<IEvaluationApi>();
    private readonly FakeApplicationTime _applicationTime = new();

    private CdiPassedEventHandler CreateSubject()
        => new(A.Dummy<ILogger<CdiPassedEventHandler>>(), _transactionSupplier, _mediator, _mapper, _publishObservability, _evaluationApi, _applicationTime);

    [Fact]
    public async Task Handle_WhenNotPerformed_DoesNothing()
    {
        // Arrange
        var request = new CDIPassedEvent
        {
            DateTime = DateTimeOffset.UtcNow
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
        Assert.Empty(context.SentMessages);
    }

    [Fact]
    public async Task Handle_WhenNo_PerformedAndNotPerformed_ThrowsException()
    {
        // Arrange
        var request = new CDIPassedEvent
        {
            DateTime = DateTimeOffset.UtcNow
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

    [Fact]
    public async Task Handle_WhenPerformed_PublishesEvents()
    {
        // Arrange
        int? evaluationId = 1;
        const int examId = 1;
        var exam = A.Fake<Core.Data.Entities.FOBT>();
        exam.FOBTId = examId;
        exam.EvaluationId = evaluationId;
        var examStatuses = new List<FOBTStatus>
        {
            new()
            {
                FOBTStatusCodeId = FOBTStatusCode.FOBTPerformed.FOBTStatusCodeId
            },
            new()
            {
                FOBTStatusCodeId = FOBTStatusCode.ValidLabResultsReceived.FOBTStatusCodeId
            }
        };
        var message = new CDIPassedEvent
        {
            EvaluationId = evaluationId!.Value,
            DateTime = DateTimeOffset.UtcNow,
            RequestId = Guid.NewGuid()
        };

        A.CallTo(() => _mediator.Send(A<GetFOBT>._, A<CancellationToken>._))
            .Returns(exam);
        A.CallTo(() => _mediator.Send(A<QueryExamStatuses>._, A<CancellationToken>._))
            .Returns(examStatuses);
        var context = new TestableMessageHandlerContext();

        // Act
        await CreateSubject().Handle(message, context);

        // Assert
        _transactionSupplier.AssertCommit();
        A.CallTo(() => _publishObservability.Commit()).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<UpdateExamStatus>.That.Matches(s =>
                s.ExamStatus.StatusCode.FOBTStatusCodeId == FOBTStatusCode.CdiPassedReceived.FOBTStatusCodeId &&
                s.ExamStatus.EvaluationId == evaluationId &&
                s.ExamStatus.StatusDateTime == message.DateTime &&
                ((ProviderPayStatusEvent)s.ExamStatus).ParentCdiEvent == nameof(CDIPassedEvent)),
            A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        var providerPayRequest = context.FindSentMessage<ProviderPayRequest>();
        Assert.NotNull(providerPayRequest);
        Assert.NotEmpty(providerPayRequest.AdditionalDetails);
    }
}