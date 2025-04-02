using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NsbEventHandlers;
using NServiceBus.Testing;
using Signify.Dps.Observability.Library.Services;
using Signify.uACR.Core.BusinessRules;
using Signify.uACR.Core.Commands;
using Signify.uACR.Core.Data.Entities;
using Signify.uACR.Core.Exceptions;
using Signify.uACR.Core.FeatureFlagging;
using Signify.uACR.Core.Models;
using Signify.uACR.Core.Queries;
using UacrNsbEvents;
using Xunit;

using PdfDeliveredToClient = UacrEvents.PdfDeliveredToClient;
using PdfEntity = Signify.uACR.Core.Data.Entities.PdfDeliveredToClient;
namespace Signify.uACR.Core.Tests.EventHandlers.Nsb;

public class PdfDeliveredToClientHandlerTests
{
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly IFeatureFlags _featureFlags = A.Fake<IFeatureFlags>();
    private readonly FakeTransactionSupplier _transactionSupplier = new();
    private readonly IBillableRules _billableRules = A.Fake<IBillableRules>();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();
    private readonly FakeApplicationTime _applicationTime = new();

    private PdfDeliveredToClientHandler CreateSubject()
        => new(A.Dummy<ILogger<PdfDeliveredToClientHandler>>(),_transactionSupplier, _mediator, _billableRules, _publishObservability, _featureFlags, _applicationTime);
    
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
        _transactionSupplier.AssertRollback();
        A.CallTo(() => _mediator.Send(A<AddPdfDeliveredToClient>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_WhenNoPdfDeliveryInDb_HappyPath(bool isBillable)
    {
        // Arrange
        const long evaluationId = 1;
        const int pdfDeliveryId = 2;
        A.CallTo(() => _mediator.Send(A<QueryPdfDeliveredToClient>._, A<CancellationToken>._))
            .Returns(new QueryPdfDeliveredToClientResult(null));
        A.CallTo(() => _mediator.Send(A<AddPdfDeliveredToClient>._, A<CancellationToken>._))
            .Returns(new PdfEntity
            {
                EvaluationId = evaluationId,
                PdfDeliveredToClientId = pdfDeliveryId
            });
        A.CallTo(() => _featureFlags.EnableBilling).Returns(true);
        
        A.CallTo(() => _featureFlags.EnableDirectBilling).Returns(false);
        
        var request = A.Fake<PdfDeliveredToClient>();
        var context = new TestableMessageHandlerContext();
        var status = A.Fake<BusinessRuleStatus>();
        status.IsMet = isBillable;
        A.CallTo(() => _billableRules.IsBillable(A<BillableRuleAnswers>._)).Returns(status);

        A.CallTo(() => _mediator.Send(A<QueryExamNotPerformed>._, A<CancellationToken>._))
            .Returns((ExamNotPerformed)null);
        
        // Act
        await CreateSubject().Handle(request, context);

        // Assert
        _transactionSupplier.AssertCommit();
        A.CallTo(() => _mediator.Send(A<AddPdfDeliveredToClient>._, A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<QueryLabResultByEvaluationId>._, A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => _billableRules.IsBillable(A<BillableRuleAnswers>._))
            .MustHaveHappened();
        Assert.Equal(2, context.SentMessages.Length);
        var processBillingEvent = context.FindSentMessage<ProcessBillingEvent>();
        Assert.Equal(isBillable, processBillingEvent.IsBillable);
        var billableExamStatusEvent = context.FindSentMessage<BillableExamStatusEvent>();
        Assert.Equal(ExamStatusCode.ClientPdfDelivered, billableExamStatusEvent.StatusCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_WhenNoPdfDeliveryInDbAndExamNotPerformed_HappyPath(bool isBillable)
    {
        // Arrange
        const long evaluationId = 1;
        const int pdfDeliveryId = 2;
        A.CallTo(() => _mediator.Send(A<QueryPdfDeliveredToClient>._, A<CancellationToken>._))
            .Returns(new QueryPdfDeliveredToClientResult(null));
        A.CallTo(() => _mediator.Send(A<AddPdfDeliveredToClient>._, A<CancellationToken>._))
            .Returns(new PdfEntity
            {
                EvaluationId = evaluationId,
                PdfDeliveredToClientId = pdfDeliveryId
            });
        A.CallTo(() => _featureFlags.EnableBilling).Returns(true);
        var request = A.Fake<PdfDeliveredToClient>();
        var context = new TestableMessageHandlerContext();
        var status = A.Fake<BusinessRuleStatus>();
        status.IsMet = isBillable;
        A.CallTo(() => _billableRules.IsBillable(A<BillableRuleAnswers>._)).Returns(status);

        A.CallTo(() => _mediator.Send(A<QueryExamNotPerformed>._, A<CancellationToken>._))
            .Returns(new ExamNotPerformed());
        
        // Act
        await CreateSubject().Handle(request, context);

        // Assert
        _transactionSupplier.AssertCommit();
        A.CallTo(() => _mediator.Send(A<AddPdfDeliveredToClient>._, A<CancellationToken>._))
            .MustHaveHappened();
        
        var billableExamStatusEvent = context.FindSentMessage<BillableExamStatusEvent>();
        Assert.Equal(ExamStatusCode.ClientPdfDelivered, billableExamStatusEvent.StatusCode);
        
        A.CallTo(() => _mediator.Send(A<QueryLabResultByEvaluationId>._, A<CancellationToken>._))
            .MustNotHaveHappened();
        A.CallTo(() => _billableRules.IsBillable(A<BillableRuleAnswers>._))
            .MustNotHaveHappened();
    }
    
    [Fact]
    public async Task Handle_Should_Throw_When_ExamNotFound_InDb()
    {
        // Arrange
        const long evaluationId = 1;
        const int pdfDeliveryId = 2;
        A.CallTo(() => _mediator.Send(A<QueryPdfDeliveredToClient>._, A<CancellationToken>._))
            .Returns(new QueryPdfDeliveredToClientResult(null));
        A.CallTo(() => _mediator.Send(A<AddPdfDeliveredToClient>._, A<CancellationToken>._))
            .Returns(new PdfEntity
            {
                EvaluationId = evaluationId,
                PdfDeliveredToClientId = pdfDeliveryId
            });
        A.CallTo(() => _mediator.Send(A<QueryExamByEvaluation>._, A<CancellationToken>._)).Returns((Exam)null);
        var request = A.Fake<PdfDeliveredToClient>();
        var context = new TestableMessageHandlerContext();

        // Act
        await Assert.ThrowsAsync<ExamNotFoundException>(async () => await CreateSubject().Handle(request, context));

        // Assert
        _transactionSupplier.AssertRollback();
        Assert.Empty(context.SentMessages);
        A.CallTo(() => _mediator.Send(A<QueryPdfDeliveredToClient>._, A<CancellationToken>._))
            .MustHaveHappened();    
        A.CallTo(() => _mediator.Send(A<AddPdfDeliveredToClient>._, A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<QueryLabResultByEvaluationId>._, A<CancellationToken>._))
            .MustNotHaveHappened();
        A.CallTo(() => _billableRules.IsBillable(A<BillableRuleAnswers>._))
            .MustNotHaveHappened();
    }
    
    [Fact]
    public async Task Should_Not_Handle_Billing_When_Feature_Flag_Is_False()
    {
        // Arrange
        const long evaluationId = 1;
        const int pdfDeliveryId = 2;
        A.CallTo(() => _mediator.Send(A<QueryPdfDeliveredToClient>._, A<CancellationToken>._))
            .Returns(new QueryPdfDeliveredToClientResult(null));
        A.CallTo(() => _mediator.Send(A<AddPdfDeliveredToClient>._, A<CancellationToken>._))
            .Returns(new PdfEntity
            {
                EvaluationId = evaluationId,
                PdfDeliveredToClientId = pdfDeliveryId
            });
        A.CallTo(() => _featureFlags.EnableBilling).Returns(false);
        var request = A.Fake<PdfDeliveredToClient>();
        var context = new TestableMessageHandlerContext();

        // Act
        await CreateSubject().Handle(request, context);

        // Assert
        _transactionSupplier.AssertCommit();
        
        A.CallTo(() => _mediator.Send(A<AddPdfDeliveredToClient>._, A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<QueryLabResultByEvaluationId>._, A<CancellationToken>._))
            .MustNotHaveHappened();
        A.CallTo(() => _billableRules.IsBillable(A<BillableRuleAnswers>._))
            .MustNotHaveHappened();
        Assert.Single(context.SentMessages);
    }
    
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_WhenNoPdfDeliveryInDb_Direct_Billing_HappyPath(bool isBillable)
    {
        // Arrange
        const long evaluationId = 1;
        const int pdfDeliveryId = 2;
        A.CallTo(() => _mediator.Send(A<QueryPdfDeliveredToClient>._, A<CancellationToken>._))
            .Returns(new QueryPdfDeliveredToClientResult(null));
        A.CallTo(() => _mediator.Send(A<AddPdfDeliveredToClient>._, A<CancellationToken>._))
            .Returns(new PdfEntity
            {
                EvaluationId = evaluationId,
                PdfDeliveredToClientId = pdfDeliveryId
            });
        
        A.CallTo(() => _featureFlags.EnableDirectBilling).Returns(true);
        
        A.CallTo(() => _featureFlags.EnableBilling).Returns(true);
        
        var request = A.Fake<PdfDeliveredToClient>();
        var context = new TestableMessageHandlerContext();
        var status = A.Fake<BusinessRuleStatus>();
        status.IsMet = isBillable;
        A.CallTo(() => _billableRules.IsBillable(A<BillableRuleAnswers>._)).Returns(status);

        A.CallTo(() => _mediator.Send(A<QueryExamNotPerformed>._, A<CancellationToken>._))
            .Returns((ExamNotPerformed)null);
        
        // Act
        await CreateSubject().Handle(request, context);

        // Assert
        _transactionSupplier.AssertCommit();
        A.CallTo(() => _mediator.Send(A<AddPdfDeliveredToClient>._, A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<QueryLabResultByEvaluationId>._, A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => _billableRules.IsBillable(A<BillableRuleAnswers>._))
            .MustHaveHappened();
        Assert.Equal(3, context.SentMessages.Length);
        
        var processLeftBillingEvent = context.SentMessages[1].Message<ProcessBillingEvent>();
        Assert.True(processLeftBillingEvent.IsBillable);
        
        var processResultBillingEvent = context.SentMessages[2].Message<ProcessBillingEvent>();
        Assert.Equal(isBillable, processResultBillingEvent.IsBillable);
        
        var billableExamStatusEvent = context.FindSentMessage<BillableExamStatusEvent>();
        Assert.Equal(ExamStatusCode.ClientPdfDelivered, billableExamStatusEvent.StatusCode);
    }
    
    [Fact]
    public async Task Should_Not_Handle_Direct_Billing_When_Feature_Flag_Is_False()
    {
        // Arrange
        const long evaluationId = 1;
        const int pdfDeliveryId = 2;
        A.CallTo(() => _mediator.Send(A<QueryPdfDeliveredToClient>._, A<CancellationToken>._))
            .Returns(new QueryPdfDeliveredToClientResult(null));
        A.CallTo(() => _mediator.Send(A<AddPdfDeliveredToClient>._, A<CancellationToken>._))
            .Returns(new PdfEntity
            {
                EvaluationId = evaluationId,
                PdfDeliveredToClientId = pdfDeliveryId
            });
        
        A.CallTo(() => _featureFlags.EnableDirectBilling).Returns(false);
        
        var request = A.Fake<PdfDeliveredToClient>();
        var context = new TestableMessageHandlerContext();

        // Act
        await CreateSubject().Handle(request, context);

        // Assert
        _transactionSupplier.AssertCommit();
        A.CallTo(() => _mediator.Send(A<AddPdfDeliveredToClient>._, A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<QueryLabResultByEvaluationId>._, A<CancellationToken>._))
            .MustNotHaveHappened();
        A.CallTo(() => _billableRules.IsBillable(A<BillableRuleAnswers>._))
            .MustNotHaveHappened();
        Assert.Single(context.SentMessages);
    }
}