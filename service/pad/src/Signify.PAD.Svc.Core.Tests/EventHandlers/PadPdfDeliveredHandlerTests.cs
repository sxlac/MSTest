using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Data.Entities;
using Signify.PAD.Svc.Core.EventHandlers;
using Signify.PAD.Svc.Core.Events;
using Signify.PAD.Svc.Core.Exceptions;
using Signify.PAD.Svc.Core.Models;
using Signify.PAD.Svc.Core.Queries;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.EventHandlers;

public class PadPdfDeliveredHandlerTests
{
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly FakeTransactionSupplier _transactionSupplier = new();

    public PadPdfDeliveredHandlerTests()
    {
        SetupPdfDelivery(false);
    }

    private PadPdfDeliveredHandler CreateSubject()
        => new(A.Dummy<ILogger<PadPdfDeliveredHandler>>(), _mapper, _mediator, _transactionSupplier);

    private void SetupPdfDelivery(bool exists)
    {
        A.CallTo(() => _mediator.Send(A<QueryPdfDeliveredToClient>._, A<CancellationToken>._))
            .Returns(new QueryPdfDeliveredToClientResult(exists ? new PDFToClient() : null));
    }

    [Fact]
    public async Task Handle_WhenPdfDeliveryEntityExists_DoesNothing()
    {
        // Arrange
        const int evaluationId = 1;

        SetupPdfDelivery(true);

        var request = new PdfDeliveredToClient
        {
            EvaluationId = evaluationId
        };

        var context = new TestableMessageHandlerContext();

        // Act
        await CreateSubject().Handle(request, context);

        // Assert
        A.CallTo(() => _mediator.Send(A<QueryPdfDeliveredToClient>.That.Matches(q =>
                    q.EvaluationId == evaluationId),
                A<CancellationToken>._))
            .MustHaveHappened();

        Assert.Empty(context.SentMessages);

        _transactionSupplier.AssertNoTransactionCreated();
    }

    [Fact]
    public async Task Handle_WhenExamNotFound_Throws()
    {
        // Arrange
        const int evaluationId = 1;

        var request = new PdfDeliveredToClient
        {
            EvaluationId = evaluationId
        };

        A.CallTo(() => _mediator.Send(A<GetPAD>._, A<CancellationToken>._))
            .Returns((Core.Data.Entities.PAD)null);

        var context = new TestableMessageHandlerContext();

        // Act
        // Assert
        await Assert.ThrowsAnyAsync<ExamNotFoundException>(async () => await CreateSubject().Handle(request, context));

        Assert.Empty(context.SentMessages);

        _transactionSupplier.AssertRollback();
    }

    [Fact]
    public async Task Handle_WhenPerformedOrNotPerformedStatusNotFound_ThrowsException()
    {
        // Arrange
        var request = new PdfDeliveredToClient();

        A.CallTo(() => _mediator.Send(A<QueryPadPerformedStatus>._, A<CancellationToken>._))
            .Returns(new QueryPadPerformedStatusResult(null));

        var context = new TestableMessageHandlerContext();

        // Act
        // Assert
        await Assert.ThrowsAnyAsync<UnableToDetermineBillabilityException>(() =>
            CreateSubject().Handle(request, context));

        Assert.Empty(context.SentMessages);

        _transactionSupplier.AssertRollback();
    }

    [Fact]
    public async Task Handle_WhenNotPerformed_SavesPdfAndBillRequestNotSent()
    {
        // Arrange
        var request = new PdfDeliveredToClient();

        A.CallTo(() => _mediator.Send(A<QueryPadPerformedStatus>._, A<CancellationToken>._))
            .Returns(new QueryPadPerformedStatusResult(false));

        var context = new TestableMessageHandlerContext();

        // Act
        await CreateSubject().Handle(request, context);

        // Assert
        A.CallTo(() => _mapper.Map<CreateOrUpdatePDFToClient>(A<PdfDeliveredToClient>._))
            .MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<CreateOrUpdatePDFToClient>._, A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<ExamStatusEventNew>.That.Matches(e =>
                e.StatusCode == StatusCodes.BillRequestNotSent), A<CancellationToken>._))
            .MustHaveHappened();

        Assert.Empty(context.PublishedMessages);
        Assert.Empty(context.SentMessages);

        _transactionSupplier.AssertCommit();
    }

    [Fact]
    public async Task Handle_WhenPerformed_SavesPdfAndBillableEventReceived()
    {
        // Arrange
        var request = new PdfDeliveredToClient();

        A.CallTo(() => _mediator.Send(A<QueryPadPerformedStatus>._, A<CancellationToken>._))
            .Returns(new QueryPadPerformedStatusResult(true));

        var context = new TestableMessageHandlerContext();

        // Act
        await CreateSubject().Handle(request, context);

        // Assert
        A.CallTo(() => _mapper.Map<CreateOrUpdatePDFToClient>(A<PdfDeliveredToClient>._))
            .MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<CreateOrUpdatePDFToClient>._, A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<ExamStatusEventNew>.That.Matches(e =>
                e.StatusCode == StatusCodes.BillableEventReceived), A<CancellationToken>._))
            .MustHaveHappened();

        Assert.Single(context.SentMessages);

        var rcm = context.FindSentMessage<RcmBillingRequest>();

        Assert.NotNull(rcm);

        _transactionSupplier.AssertCommit();
    }
}
