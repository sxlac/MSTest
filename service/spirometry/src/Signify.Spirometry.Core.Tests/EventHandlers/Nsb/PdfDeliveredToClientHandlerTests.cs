using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NsbEventHandlers;
using NServiceBus.Testing;
using Signify.AkkaStreams.Kafka.Persistence;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using Signify.Spirometry.Core.Commands;
using Signify.Spirometry.Core.Data;
using Signify.Spirometry.Core.Infrastructure;
using Signify.Spirometry.Core.Queries;
using SpiroNsb.SagaEvents;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

using PdfDeliveredToClient = SpiroEvents.PdfDeliveredToClient;
using PdfEntity = Signify.Spirometry.Core.Data.Entities.PdfDeliveredToClient;

namespace Signify.Spirometry.Core.Tests.EventHandlers.Nsb;

public class PdfDeliveredToClientHandlerTests
{
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly ITransactionSupplier _transactionSupplier = A.Fake<ITransactionSupplier>();
    private readonly IApplicationTime _applicationTime = new FakeApplicationTime();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();
        
    private PdfDeliveredToClientHandler CreateSubject()
        => new PdfDeliveredToClientHandler(A.Dummy<ILogger<PdfDeliveredToClientHandler>>(),
            _mediator, 
            _transactionSupplier, 
            _applicationTime,
            _publishObservability);

    [Fact]
    public async Task Handle_WhenAlreadyHasPdfDeliveryInDb_DoesNothing()
    {
        // Arrange
        const long evaluationId = 1;

        var request = new PdfDeliveredToClient
        {
            EvaluationId = evaluationId
        };

        A.CallTo(() => _mediator.Send(A<QueryPdfDeliveredToClient>._, A<CancellationToken>._))
            .Returns(new QueryPdfDeliveredToClientResult(new PdfEntity()));

        var context = new TestableMessageHandlerContext();

        // Act
        await CreateSubject().Handle(request, context);

        // Assert
        A.CallTo(() => _mediator.Send(A<QueryPdfDeliveredToClient>.That.Matches(q =>
                    q.EvaluationId == evaluationId),
                A<CancellationToken>._))
            .MustHaveHappened();

        A.CallTo(() => _transactionSupplier.BeginTransaction())
            .MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<AddPdfDeliveredToClient>._, A<CancellationToken>._))
            .MustNotHaveHappened();

        Assert.Empty(context.SentMessages);
        Assert.Empty(context.PublishedMessages);
            
        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>._, true)).MustNotHaveHappened();
    }

    [Fact]
    public async Task Handle_WhenNoPdfDeliveryInDb_HappyPath()
    {
        // Arrange
        const long evaluationId = 1;
        const int pdfDeliveryId = 2;

        A.CallTo(() => _mediator.Send(A<QueryPdfDeliveredToClient>._, A<CancellationToken>._))
            .Returns(new QueryPdfDeliveredToClientResult(null));

        var transaction = A.Fake<IBufferedTransaction>();
        A.CallTo(() => _transactionSupplier.BeginTransaction())
            .Returns(transaction);

        A.CallTo(() => _mediator.Send(A<AddPdfDeliveredToClient>._, A<CancellationToken>._))
            .Returns(new PdfEntity
            {
                EvaluationId = evaluationId,
                PdfDeliveredToClientId = pdfDeliveryId
            });

        var request = new PdfDeliveredToClient();

        var context = new TestableMessageHandlerContext();

        // Act
        await CreateSubject().Handle(request, context);

        // Assert
        A.CallTo(() => _transactionSupplier.BeginTransaction())
            .MustHaveHappened();

        A.CallTo(() => _mediator.Send(A<AddPdfDeliveredToClient>._, A<CancellationToken>._))
            .MustHaveHappened();

        Assert.Single(context.SentMessages);

        var message = context.FindSentMessage<PdfDeliveredToClientEvent>();

        Assert.NotNull(message);
        Assert.Equal(evaluationId, message.EvaluationId);
        Assert.Equal(pdfDeliveryId, message.PdfDeliveredToClientId);
        Assert.Equal(_applicationTime.UtcNow(), message.CreatedDateTime);

        A.CallTo(() => transaction.CommitAsync(A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => transaction.Dispose())
            .MustHaveHappened();
            
        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>._, true)).MustHaveHappenedOnceExactly();
    }
}