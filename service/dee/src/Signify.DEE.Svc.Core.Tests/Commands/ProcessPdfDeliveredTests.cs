using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.DEE.Svc.Core.BusinessRules;
using Signify.DEE.Svc.Core.Commands;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Events;
using Signify.DEE.Svc.Core.Messages.Commands;
using Signify.DEE.Svc.Core.Messages.Models;
using Signify.DEE.Svc.Core.Messages.Queries;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Commands;

public class ProcessPdfDeliveredHandlerTests
{
    private readonly ProcessPdfDeliveredHandler _processPdfDeliveredHandler;
    private readonly TestableMessageHandlerContext _messageHandlerContext;
    private readonly IMediator _mediator;
    private readonly FakeTransactionSupplier _transactionSupplier;
    private readonly IBillableRules _billableRules;
    private readonly FakeApplicationTime _applicationTime = new();

    public ProcessPdfDeliveredHandlerTests()
    {
        var logger = A.Fake<ILogger<ProcessPdfDeliveredHandler>>();
        _mediator = A.Fake<IMediator>();
        var mapper = A.Fake<IMapper>();
        _transactionSupplier = new FakeTransactionSupplier();
        _billableRules = A.Fake<IBillableRules>();
        _processPdfDeliveredHandler = new ProcessPdfDeliveredHandler(logger, _mediator, mapper, _transactionSupplier, _billableRules, _applicationTime);
        _messageHandlerContext = new TestableMessageHandlerContext();
    }

    [Fact]
    public async Task Handle_When_GetExamStatus_Throws_Exception()
    {
        //Arrange
        var message = new ProcessPdfDelivered
        {
            EventId = "Signify.Evaluation.Service",
            EvaluationId = 1,
            DeliveryDateTime = _applicationTime.UtcNow(),
            CreatedDateTime = _applicationTime.UtcNow(),
            BatchId = 1,
            BatchName = "FakeBatchName"
        };
        A.CallTo(() => _mediator.Send(A<GetExamByEvaluation>._, A<CancellationToken>._)).Returns(new Exam() { ExamId = 1, EvaluationId = 1 });
        A.CallTo(() => _mediator.Send(A<CreateStatus>._, A<CancellationToken>._)).Returns(new CreateStatusResponse(null, true));
        A.CallTo(() => _mediator.Send(A<GetAllExamStatus>._, A<CancellationToken>._)).Throws<Exception>();

        await Assert.ThrowsAnyAsync<Exception>(async () => await _processPdfDeliveredHandler.Handle(message, _messageHandlerContext));
        _transactionSupplier.AssertRollback();
        A.CallTo(() => _mediator.Send(A<GetRcmBillId>._, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task Handle_When_Not_Gradable(bool containsIncompleteStatus, bool isNotGradable)
    {
        //Arrange
        var message = new ProcessPdfDelivered
        {
            EventId = "Signify.Evaluation.Service",
            EvaluationId = 1,
            DeliveryDateTime = _applicationTime.UtcNow(),
            CreatedDateTime = _applicationTime.UtcNow(),
            BatchId = 1,
            BatchName = "FakeBatchName"
        };
        var notGradableStatus = A.Fake<BusinessRuleStatus>();
        notGradableStatus.IsMet = isNotGradable;
        var inCompleteStatus = A.Fake<BusinessRuleStatus>();
        inCompleteStatus.IsMet = containsIncompleteStatus;
        A.CallTo(() => _mediator.Send(A<GetExamByEvaluation>._, A<CancellationToken>._)).Returns(new Exam() { ExamId = 1, EvaluationId = 1 });
        A.CallTo(() => _mediator.Send(A<CreateStatus>._, A<CancellationToken>._)).Returns(new CreateStatusResponse(null, true));
        A.CallTo(() => _billableRules.IsNotGradable(A<BusinessRuleAnswers>._)).Returns(notGradableStatus);
        A.CallTo(() => _billableRules.IsIncompleteStatusPresent(A<BusinessRuleAnswers>._)).Returns(inCompleteStatus);

        await _processPdfDeliveredHandler.Handle(message, _messageHandlerContext);

        A.CallTo(() => _mediator.Send(A<GetRcmBillId>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<CreateStatus>.That.Matches(s => s.ExamStatusCode.Name == ExamStatusCode.BillRequestNotSent.Name),
            A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        _transactionSupplier.AssertCommit();
    }

    [Theory]
    [InlineData(false, false, true)]
    public async Task Handle_When_Gradable(bool containsIncompleteStatus, bool isNotGradable, bool isGradable)
    {
        //Arrange
        var message = new ProcessPdfDelivered
        {
            EventId = "Signify.Evaluation.Service",
            EvaluationId = 1,
            DeliveryDateTime = _applicationTime.UtcNow(),
            CreatedDateTime = _applicationTime.UtcNow(),
            BatchId = 1,
            BatchName = "FakeBatchName"
        };
        var notGradableStatus = A.Fake<BusinessRuleStatus>();
        notGradableStatus.IsMet = isNotGradable;
        var gradableStatus = A.Fake<BusinessRuleStatus>();
        gradableStatus.IsMet = isGradable;
        var inCompleteStatus = A.Fake<BusinessRuleStatus>();
        inCompleteStatus.IsMet = containsIncompleteStatus;
        A.CallTo(() => _mediator.Send(A<GetExamByEvaluation>._, A<CancellationToken>._)).Returns(new Exam() { ExamId = 1, AppointmentId = 12345, EvaluationId = 1, EvaluationObjectiveId = 1, EvaluationObjective = new EvaluationObjective() { EvaluationObjectiveId = 1, Objective = "Comprehensive" } });
        A.CallTo(() => _mediator.Send(A<CreateStatus>._, A<CancellationToken>._)).Returns(new CreateStatusResponse(null, true));
        A.CallTo(() => _billableRules.IsNotGradable(A<BusinessRuleAnswers>._)).Returns(notGradableStatus);
        A.CallTo(() => _billableRules.IsGradable(A<BusinessRuleAnswers>._)).Returns(gradableStatus);
        A.CallTo(() => _billableRules.IsIncompleteStatusPresent(A<BusinessRuleAnswers>._)).Returns(inCompleteStatus);
        A.CallTo(() => _mediator.Send(A<GetRcmBillId>._, A<CancellationToken>._)).Returns("");
        A.CallTo(() => _mediator.Send(A<GetAllExamStatus>._, A<CancellationToken>._)).Returns(A.Fake<List<int>>());

        await _processPdfDeliveredHandler.Handle(message, _messageHandlerContext);

        A.CallTo(() => _mediator.Send(A<GetRcmBillId>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreateStatus>.That.Matches(s => s.ExamStatusCode.Name == ExamStatusCode.BillRequestNotSent.Name),
            A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>._, A<CancellationToken>._)).MustNotHaveHappened();
        var billRequest = _messageHandlerContext.FindSentMessage<RCMBillingRequestEvent>();
        Assert.NotNull(billRequest);
        Assert.NotEmpty(billRequest.AdditionalDetails);
        
        Assert.Equal("12345", billRequest.AdditionalDetails["appointmentId"]);
        Assert.Equal("signify.dee.service", billRequest.ApplicationId);
        _transactionSupplier.AssertCommit();
    }

    [Fact]
    public async Task Should_Do_RCM_Bill_Id_Check_When_Pdf_Exists()
    {
        //Arrange
        var message = new ProcessPdfDelivered
        {
            EventId = "Signify.Evaluation.Service",
            EvaluationId = 1,
            DeliveryDateTime = _applicationTime.UtcNow(),
            CreatedDateTime = _applicationTime.UtcNow(),
            BatchId = 1,
            BatchName = "FakeBatchName"
        };
        var notGradableStatus = A.Fake<BusinessRuleStatus>();
        notGradableStatus.IsMet = false;
        var gradableStatus = A.Fake<BusinessRuleStatus>();
        gradableStatus.IsMet = true;
        var inCompleteStatus = A.Fake<BusinessRuleStatus>();
        inCompleteStatus.IsMet = false;
        A.CallTo(() => _mediator.Send(A<GetExamByEvaluation>._, A<CancellationToken>._)).Returns(new Exam { ExamId = 1, EvaluationId = 1 });
        A.CallTo(() => _mediator.Send(A<CreateStatus>._, A<CancellationToken>._)).Returns(new CreateStatusResponse(null, true));
        A.CallTo(() => _billableRules.IsNotGradable(A<BusinessRuleAnswers>._)).Returns(notGradableStatus);
        A.CallTo(() => _billableRules.IsGradable(A<BusinessRuleAnswers>._)).Returns(gradableStatus);
        A.CallTo(() => _billableRules.IsIncompleteStatusPresent(A<BusinessRuleAnswers>._)).Returns(inCompleteStatus);
        A.CallTo(() => _mediator.Send(A<GetRcmBillId>._, A<CancellationToken>._)).Returns("FakeBillId");


        await _processPdfDeliveredHandler.Handle(message, _messageHandlerContext);

        A.CallTo(() => _mediator.Send(A<GetRcmBillId>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        _transactionSupplier.AssertCommit();
    }


    [Fact]
    public async Task Should_Do_RCM_Bill_Id_Check_When_Pdf_Not_Exists_And_Create_Pdf()
    {
        //Arrange
        var message = new ProcessPdfDelivered
        {
            EventId = "Signify.Evaluation.Service",
            EvaluationId = 2,
            DeliveryDateTime = _applicationTime.UtcNow(),
            CreatedDateTime = _applicationTime.UtcNow(),
            BatchId = 1,
            BatchName = "FakeBatchName"
        };
        var notGradableStatus = A.Fake<BusinessRuleStatus>();
        notGradableStatus.IsMet = false;
        var gradableStatus = A.Fake<BusinessRuleStatus>();
        gradableStatus.IsMet = true;
        var inCompleteStatus = A.Fake<BusinessRuleStatus>();
        inCompleteStatus.IsMet = false;
        A.CallTo(() => _mediator.Send(A<GetExamByEvaluation>._, A<CancellationToken>._)).Returns(new Exam() { ExamId = 1, EvaluationId = 1 });
        A.CallTo(() => _mediator.Send(A<CreateStatus>._, A<CancellationToken>._)).Returns(new CreateStatusResponse(null, true));
        A.CallTo(() => _billableRules.IsNotGradable(A<BillableRuleAnswers>._)).Returns(notGradableStatus);
        A.CallTo(() => _billableRules.IsGradable(A<BillableRuleAnswers>._)).Returns(gradableStatus);
        A.CallTo(() => _billableRules.IsIncompleteStatusPresent(A<BillableRuleAnswers>._)).Returns(inCompleteStatus);
        A.CallTo(() => _mediator.Send(A<GetRcmBillId>._, A<CancellationToken>._)).Returns("FakeBillId");
        A.CallTo(() => _mediator.Send(A<CreateOrUpdatePdfToClient>._, A<CancellationToken>._)).Returns(new PDFToClient() { ExamId = 1, EvaluationId = 1 });

        await _processPdfDeliveredHandler.Handle(message, _messageHandlerContext);

        //Assert
        A.CallTo(() => _mediator.Send(A<GetRcmBillId>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreateOrUpdatePdfToClient>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        _transactionSupplier.AssertCommit();
    }

    [Fact]
    public async Task Should_Publish_Bill_Request_Not_Sent_When_Incomplete()
    {
        //Arrange
        var message = new ProcessPdfDelivered
        {
            EventId = "Signify.Evaluation.Service",
            EvaluationId = 3,
            DeliveryDateTime = _applicationTime.UtcNow(),
            CreatedDateTime = _applicationTime.UtcNow(),
            BatchId = 1,
            BatchName = "FakeBatchName"
        };
        var notGradableStatus = A.Fake<BusinessRuleStatus>();
        notGradableStatus.IsMet = false;
        var gradableStatus = A.Fake<BusinessRuleStatus>();
        gradableStatus.IsMet = true;
        var inCompleteStatus = A.Fake<BusinessRuleStatus>();
        inCompleteStatus.IsMet = true;
        A.CallTo(() => _mediator.Send(A<GetExamByEvaluation>._, A<CancellationToken>._)).Returns(new Exam { ExamId = 1, EvaluationId = 1 });
        A.CallTo(() => _mediator.Send(A<CreateStatus>._, A<CancellationToken>._)).Returns(new CreateStatusResponse(null, true));
        A.CallTo(() => _billableRules.IsNotGradable(A<BusinessRuleAnswers>._)).Returns(notGradableStatus);
        A.CallTo(() => _billableRules.IsGradable(A<BusinessRuleAnswers>._)).Returns(gradableStatus);
        A.CallTo(() => _billableRules.IsIncompleteStatusPresent(A<BusinessRuleAnswers>._)).Returns(inCompleteStatus);
        A.CallTo(() => _mediator.Send(A<GetRcmBillId>._, A<CancellationToken>._)).Returns("FakeBillId");


        await _processPdfDeliveredHandler.Handle(message, _messageHandlerContext);

        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        _transactionSupplier.AssertCommit();
    }
}