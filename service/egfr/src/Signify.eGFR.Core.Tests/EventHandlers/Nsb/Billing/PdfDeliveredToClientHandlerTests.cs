using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using EgfrNsbEvents;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NsbEventHandlers;
using NServiceBus.Testing;
using Signify.Dps.Observability.Library.Services;
using Signify.eGFR.Core.BusinessRules;
using Signify.eGFR.Core.Commands;
using Signify.eGFR.Core.Data.Entities;
using Signify.eGFR.Core.Exceptions;
using Signify.eGFR.Core.FeatureFlagging;
using Signify.eGFR.Core.Models;
using Signify.eGFR.Core.Queries;
using Xunit;
using PdfDeliveredToClient = EgfrEvents.PdfDeliveredToClient;
using PdfEntity = Signify.eGFR.Core.Data.Entities.PdfDeliveredToClient;

namespace Signify.eGFR.Core.Tests.EventHandlers.Nsb.Billing;

public class PdfDeliveredToClientHandlerTests
{
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly FakeTransactionSupplier _transactionSupplier = new();
    private readonly IBillableRules _billableRules = A.Fake<IBillableRules>();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();
    private readonly IMapper _fakeMapper = A.Fake<IMapper>();
    private readonly IFeatureFlags _featureFlags = A.Fake<IFeatureFlags>();
    private readonly FakeApplicationTime _applicationTime = new();

    private PdfDeliveredToClientHandler CreateSubject()
        => new(A.Dummy<ILogger<PdfDeliveredToClientHandler>>(), _mediator, _transactionSupplier, _publishObservability,
            _applicationTime, _billableRules,
            _fakeMapper, _featureFlags);

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
    [MemberData(nameof(LabResults))]
    public async Task Handle_WhenNoPdfDeliveryInDb_HappyPath(bool isBillable, Data.Entities.LabResult labResult, QuestLabResult questLabResult)
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
        var request = A.Fake<PdfDeliveredToClient>();
        var context = new TestableMessageHandlerContext();
        var status = A.Fake<BusinessRuleStatus>();
        status.IsMet = isBillable;
        A.CallTo(() => _billableRules.IsBillable(A<BillableRuleAnswers>._)).Returns(status);
        A.CallTo(() => _mediator.Send(A<QueryLabResultByExamId>._, A<CancellationToken>._)).Returns(labResult);
        A.CallTo(() => _mediator.Send(A<QueryQuestLabResultByEvaluationId>._, A<CancellationToken>._)).Returns(questLabResult);
        
        A.CallTo(() => _mediator.Send(A<QueryExamNotPerformed>._, A<CancellationToken>._))
            .Returns((ExamNotPerformed)null);
        
        // Act
        await CreateSubject().Handle(request, context);

        // Assert
        _transactionSupplier.AssertCommit();
        A.CallTo(() => _mediator.Send(A<AddPdfDeliveredToClient>._, A<CancellationToken>._))
            .MustHaveHappened();
        /*A.CallTo(() => _mediator.Send(A<QueryQuestLabResultByEvaluationId>._, A<CancellationToken>._))
            .MustHaveHappened();*/
        A.CallTo(() => _mediator.Send(A<QueryLabResultByExamId>._, A<CancellationToken>._))
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
    [MemberData(nameof(LabResults))]
    public async Task Handle_WhenNoPdfDeliveryInDbAndExamNotPerformed_HappyPath(bool isBillable, Data.Entities.LabResult labResult, QuestLabResult questLabResult)
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
        var request = A.Fake<PdfDeliveredToClient>();
        var context = new TestableMessageHandlerContext();
        var status = A.Fake<BusinessRuleStatus>();
        status.IsMet = isBillable;
        A.CallTo(() => _billableRules.IsBillable(A<BillableRuleAnswers>._)).Returns(status);
        A.CallTo(() => _mediator.Send(A<QueryLabResultByExamId>._, A<CancellationToken>._)).Returns(labResult);
        A.CallTo(() => _mediator.Send(A<QueryQuestLabResultByEvaluationId>._, A<CancellationToken>._)).Returns(questLabResult);
        
        A.CallTo(() => _mediator.Send(A<QueryExamNotPerformed>._, A<CancellationToken>._))
            .Returns(new ExamNotPerformed());
        
        // Act
        await CreateSubject().Handle(request, context);

        // Assert
        _transactionSupplier.AssertCommit();
        A.CallTo(() => _mediator.Send(A<AddPdfDeliveredToClient>._, A<CancellationToken>._))
            .MustHaveHappened();
        /*A.CallTo(() => _mediator.Send(A<QueryQuestLabResultByEvaluationId>._, A<CancellationToken>._))
            .MustHaveHappened();*/
        
        var billableExamStatusEvent = context.FindSentMessage<BillableExamStatusEvent>();
        Assert.Equal(ExamStatusCode.ClientPdfDelivered, billableExamStatusEvent.StatusCode);
        
        A.CallTo(() => _mediator.Send(A<QueryLabResultByExamId>._, A<CancellationToken>._))
            .MustNotHaveHappened();
        
        A.CallTo(() => _billableRules.IsBillable(A<BillableRuleAnswers>._))
            .MustNotHaveHappened();
        
        A.CallTo(() => _mediator.Send(A<ProcessBillingEvent>._, A<CancellationToken>._))
            .MustNotHaveHappened();
        
        A.CallTo(() => _billableRules.IsBillable(A<BillableRuleAnswers>._))
            .MustNotHaveHappened();
    }

    
    public static IEnumerable<object[]> LabResults()
    {
        yield return
        [
            true, 
            new Data.Entities.LabResult{
                NormalityIndicatorId = 1
            }, 
            null
        ];
        yield return
        [
            true, 
            null, 
            new QuestLabResult{
                NormalityCode = "N"
            }
        ];
        yield return
        [
            false, 
            new Data.Entities.LabResult{
                NormalityIndicatorId = 1
            }, 
            null
        ];
        yield return
        [
            false, 
            null, 
            new QuestLabResult{
                NormalityCode = "N"
            }
        ];
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
        A.CallTo(() => _mediator.Send(A<QueryExam>._, A<CancellationToken>._)).Returns((Exam)null);
        var request = A.Fake<PdfDeliveredToClient>();
        var context = new TestableMessageHandlerContext();

        // Act
        await Assert.ThrowsAsync<ExamNotFoundByEvaluationException>(async () => await CreateSubject().Handle(request, context));

        // Assert
        _transactionSupplier.AssertRollback();
        Assert.Empty(context.SentMessages);
        A.CallTo(() => _mediator.Send(A<QueryPdfDeliveredToClient>._, A<CancellationToken>._))
            .MustHaveHappened();    
        A.CallTo(() => _mediator.Send(A<AddPdfDeliveredToClient>._, A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<QueryQuestLabResultByEvaluationId>._, A<CancellationToken>._))
            .MustNotHaveHappened();
        A.CallTo(() => _billableRules.IsBillable(A<BillableRuleAnswers>._))
            .MustNotHaveHappened();
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
        A.CallTo(() => _mediator.Send(A<QueryLabResultByExamId>._, A<CancellationToken>._))
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
        
        var request = A.Fake<PdfDeliveredToClient>();
        var context = new TestableMessageHandlerContext();

        A.CallTo(() => _featureFlags.EnableDirectBilling).Returns(false);
        
        // Act
        await CreateSubject().Handle(request, context);

        // Assert
        _transactionSupplier.AssertCommit();
        A.CallTo(() => _mediator.Send(A<AddPdfDeliveredToClient>._, A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<QueryLabResultByExamId>._, A<CancellationToken>._))
            .MustNotHaveHappened();
        A.CallTo(() => _billableRules.IsBillable(A<BillableRuleAnswers>._))
            .MustNotHaveHappened();
        Assert.Single(context.SentMessages);
    }
}