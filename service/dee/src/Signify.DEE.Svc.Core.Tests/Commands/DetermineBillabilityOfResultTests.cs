using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using Iris.Public.Types.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.DEE.Messages.Status;
using Signify.DEE.Svc.Core.BusinessRules;
using Signify.DEE.Svc.Core.Commands;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Events;
using Signify.DEE.Svc.Core.FeatureFlagging;
using Signify.DEE.Svc.Core.Messages.Commands;
using Signify.DEE.Svc.Core.Messages.Models;
using Signify.DEE.Svc.Core.Messages.Queries;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Commands;

public class DetermineBillabilityOfResultHandlerTests
{
    private readonly IMediator _mediator;
    private readonly TestableMessageHandlerContext _messageHandlerContext;
    private readonly DetermineBillabityOfResultHandler _determineBillabilityOfResultHandler;
    private readonly FakeApplicationTime _applicationTime = new();
    private readonly IFeatureFlags _featureFlags;
    private readonly IBillableRules _billableRules;

    public DetermineBillabilityOfResultHandlerTests()
    {
        var log = A.Fake<ILogger<DetermineBillabityOfResultHandler>>();
        _mediator = A.Fake<IMediator>();
        var mapper = A.Fake<IMapper>();
        var transactionSupplier = A.Fake<ITransactionSupplier>();
        _billableRules = A.Fake<IBillableRules>();
        _featureFlags = A.Fake<IFeatureFlags>();
        _determineBillabilityOfResultHandler = new DetermineBillabityOfResultHandler(log, _mediator, mapper, transactionSupplier, _applicationTime, _billableRules, _featureFlags);
        _messageHandlerContext = new TestableMessageHandlerContext();
    }

    [Fact]
    public async Task Should_Send_Bill_Request_To_Rcm_Exam_Gradable_And_Billable()
    {
        //// Arrange
        var exam = new ExamModel
        {
            ClientId = 31,
            CreatedDateTime = _applicationTime.UtcNow(),
            DateOfService = _applicationTime.UtcNow(),
            EvaluationId = 45332,
            ExamId = 74632,
            ProviderId = 34567,
            EvaluationObjective = new EvaluationObjective() { EvaluationObjectiveId = 1, Objective = "Comprehensive" }
        };
        var imageDetails = new ResultImageDetails
        {
            LeftEyeOriginalCount = 1,
            RightEyeOriginalCount = 1
        };
        var gradings = new ResultGrading();
        var pdfToClient = new PdfToClientModel { EvaluationId = 45332 };
        var message = new DetermineBillabityOfResult { Exam = exam, ImageDetails = imageDetails, Gradings = gradings };
        var businessRuleStatus = A.Fake<BusinessRuleStatus>();
        businessRuleStatus.IsMet = true;

        A.CallTo(() => _billableRules.IsGradable(A<BillableRuleAnswers>._)).Returns(businessRuleStatus);
        A.CallTo(() => _billableRules.IsBillable(A<BillableRuleAnswers>._)).Returns(businessRuleStatus);
        A.CallTo(() => _mediator.Send(A<CreateStatus>._, A<CancellationToken>._)).Returns(new CreateStatusResponse(null, true));
        A.CallTo(() => _mediator.Send(A<UpdateExamGrade>._, A<CancellationToken>._)).Returns(Unit.Value);
        A.CallTo(() => _mediator.Send(A<GetPdfToClient>._, A<CancellationToken>._)).Returns(pdfToClient);
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>._, A<CancellationToken>._)).Returns(Unit.Value);
        A.CallTo(() => _mediator.Send(A<GetRcmBillId>._, A<CancellationToken>._)).Returns("");

        // Act
        await _determineBillabilityOfResultHandler.Handle(message, _messageHandlerContext);

        // Assert
        A.CallTo(() => _billableRules.IsGradable(A<BillableRuleAnswers>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _billableRules.IsBillable(A<BillableRuleAnswers>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreateStatus>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<UpdateExamGrade>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<GetPdfToClient>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<GetRcmBillId>._, A<CancellationToken>._)).MustHaveHappened();
        _messageHandlerContext.SentMessages.Length.Should().Be(1);
        Assert.IsType<RCMBillingRequestEvent>(_messageHandlerContext.SentMessages[0].Message);
        var sentMessages = (RCMBillingRequestEvent)_messageHandlerContext.SentMessages[0].Message;
        Assert.Equal(exam.EvaluationId, sentMessages.EvaluationId);
    }

    [Fact]
    public async Task Should_Publish_Bill_Request_Not_Sent_Status_Exam_Gradable_Not_Billable()
    {
        //// Arrange
        var exam = new ExamModel { ClientId = 43, CreatedDateTime = _applicationTime.UtcNow(), DateOfService = _applicationTime.UtcNow(), EvaluationId = 45632, ExamId = 45643, ProviderId = 56767 };
        var imageDetails = new ResultImageDetails
        {
            LeftEyeOriginalCount = 0,
            RightEyeOriginalCount = 1
        };
        var gradings = new ResultGrading();
        var message = new DetermineBillabityOfResult { Exam = exam, ImageDetails = imageDetails, Gradings = gradings };
        var pdfToClient = new PdfToClientModel { EvaluationId = 45632 };
        var gradableStatus = A.Fake<BusinessRuleStatus>();
        gradableStatus.IsMet = true;
        var billableStatus = A.Fake<BusinessRuleStatus>();
        billableStatus.IsMet = false;

        A.CallTo(() => _mediator.Send(A<CreateStatus>._, A<CancellationToken>._)).Returns(new CreateStatusResponse(null, true));
        A.CallTo(() => _mediator.Send(A<UpdateExamGrade>._, A<CancellationToken>._)).Returns(Unit.Value);
        A.CallTo(() => _mediator.Send(A<GetPdfToClient>._, A<CancellationToken>._)).Returns(pdfToClient);
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>._, A<CancellationToken>._)).Returns(Unit.Value);
        A.CallTo(() => _mediator.Send(A<GetRcmBillId>._, A<CancellationToken>._)).Returns("");
        A.CallTo(() => _billableRules.IsGradable(A<BillableRuleAnswers>._)).Returns(gradableStatus);
        A.CallTo(() => _billableRules.IsBillable(A<BillableRuleAnswers>._)).Returns(billableStatus);

        // Act
        await _determineBillabilityOfResultHandler.Handle(message, _messageHandlerContext);

        // Assert
        A.CallTo(() => _billableRules.IsGradable(A<BillableRuleAnswers>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _billableRules.IsBillable(A<BillableRuleAnswers>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreateStatus>.That.Matches(s => s.ExamStatusCode.Name == ExamStatusCode.Incomplete.Name), A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreateStatus>.That.Matches(s => s.ExamStatusCode.Name == ExamStatusCode.Gradable.Name), A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreateStatus>.That.Matches(s => s.ExamStatusCode.Name == ExamStatusCode.BillRequestNotSent.Name), A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<UpdateExamGrade>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<GetPdfToClient>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>.That.Matches(s => s.Status is BillRequestNotSent), A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<GetRcmBillId>._, A<CancellationToken>._)).MustNotHaveHappened();
        _messageHandlerContext.SentMessages.Length.Should().Be(0);
    }

    [Fact]
    public async Task Should_Publish_Bill_Request_Not_Sent_Status_Exam_Not_Gradable_Not_Billable()
    {
        //// Arrange
        var exam = new ExamModel { ClientId = 54, CreatedDateTime = _applicationTime.UtcNow(), DateOfService = _applicationTime.UtcNow(), EvaluationId = 56767, ExamId = 1, ProviderId = 1 };
        var imageDetails = new ResultImageDetails
        {
            LeftEyeOriginalCount = 0,
            RightEyeOriginalCount = 1
        };
        var gradings = new ResultGrading();
        var message = new DetermineBillabityOfResult { Exam = exam, ImageDetails = imageDetails, Gradings = gradings };
        var pdfToClient = new PdfToClientModel { EvaluationId = 1 };
        var businessRuleStatus = A.Fake<BusinessRuleStatus>();
        businessRuleStatus.IsMet = false;

        A.CallTo(() => _billableRules.IsGradable(A<BillableRuleAnswers>._)).Returns(businessRuleStatus);
        A.CallTo(() => _billableRules.IsBillable(A<BillableRuleAnswers>._)).Returns(businessRuleStatus);
        A.CallTo(() => _mediator.Send(A<CreateStatus>._, A<CancellationToken>._)).Returns(new CreateStatusResponse(null, true));
        A.CallTo(() => _mediator.Send(A<UpdateExamGrade>._, A<CancellationToken>._)).Returns(Unit.Value);
        A.CallTo(() => _mediator.Send(A<GetPdfToClient>._, A<CancellationToken>._)).Returns(pdfToClient);
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>._, A<CancellationToken>._)).Returns(Unit.Value);
        A.CallTo(() => _mediator.Send(A<GetRcmBillId>._, A<CancellationToken>._)).Returns("");

        // Act
        await _determineBillabilityOfResultHandler.Handle(message, _messageHandlerContext);

        // Assert
        A.CallTo(() => _billableRules.IsGradable(A<BillableRuleAnswers>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _billableRules.IsBillable(A<BillableRuleAnswers>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreateStatus>.That.Matches(s => s.ExamStatusCode.Name == ExamStatusCode.NotGradable.Name), A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreateStatus>.That.Matches(s => s.ExamStatusCode.Name == ExamStatusCode.BillRequestNotSent.Name), A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<UpdateExamGrade>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<GetPdfToClient>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>.That.Matches(s => s.Status is BillRequestNotSent), A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<GetRcmBillId>._, A<CancellationToken>._)).MustNotHaveHappened();
        _messageHandlerContext.SentMessages.Length.Should().Be(0);
    }

    [Fact]
    public async Task Should_Only_Add_Statuses_When_Pdf_Not_Received_Yet()
    {
        //// Arrange
        var exam = new ExamModel { ClientId = 54, CreatedDateTime = _applicationTime.UtcNow(), DateOfService = _applicationTime.UtcNow(), EvaluationId = 56743, ExamId = 67845, ProviderId = 34534 };
        var imageDetails = new ResultImageDetails
        {
            LeftEyeOriginalCount = 1,
            RightEyeOriginalCount = 1
        };
        var gradings = new ResultGrading();
        var pdfToClient = new PdfToClientModel();
        var businessRuleStatus = A.Fake<BusinessRuleStatus>();
        businessRuleStatus.IsMet = true;

        var message = new DetermineBillabityOfResult { Exam = exam, ImageDetails = imageDetails, Gradings = gradings };

        A.CallTo(() => _billableRules.IsGradable(A<BillableRuleAnswers>._)).Returns(businessRuleStatus);
        A.CallTo(() => _billableRules.IsBillable(A<BillableRuleAnswers>._)).Returns(businessRuleStatus);
        A.CallTo(() => _mediator.Send(A<CreateStatus>._, A<CancellationToken>._)).Returns(new CreateStatusResponse(null, true));
        A.CallTo(() => _mediator.Send(A<UpdateExamGrade>._, A<CancellationToken>._)).Returns(Unit.Value);
        A.CallTo(() => _mediator.Send(A<GetPdfToClient>._, A<CancellationToken>._)).Returns(pdfToClient);
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>._, A<CancellationToken>._)).Returns(Unit.Value);
        A.CallTo(() => _mediator.Send(A<GetRcmBillId>._, A<CancellationToken>._)).Returns("");

        // Act
        await _determineBillabilityOfResultHandler.Handle(message, _messageHandlerContext);

        // Assert
        A.CallTo(() => _billableRules.IsGradable(A<BillableRuleAnswers>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _billableRules.IsBillable(A<BillableRuleAnswers>._)).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<CreateStatus>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<UpdateExamGrade>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<GetPdfToClient>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<GetRcmBillId>._, A<CancellationToken>._)).MustNotHaveHappened();
        _messageHandlerContext.SentMessages.Length.Should().Be(0);
    }

    [Fact]
    public async Task Should_Process_Payment_Exam_Gradable_And_CdiEvent_Received()
    {
        //// Arrange
        var exam = new ExamModel
        {
            AppointmentId = 12345,
            ClientId = 31,
            CreatedDateTime = _applicationTime.UtcNow(),
            DateOfService = _applicationTime.UtcNow(),
            EvaluationId = 45332,
            ExamId = 74632,
            ProviderId = 34567,
            EvaluationObjective = new EvaluationObjective() { EvaluationObjectiveId = 1, Objective = "Comprehensive" }
        };
        var imageDetails = new ResultImageDetails
        {
            LeftEyeOriginalCount = 1,
            RightEyeOriginalCount = 1
        };
        var gradings = new ResultGrading();
        var pdfToClient = new PdfToClientModel { EvaluationId = 45332 };
        var message = new DetermineBillabityOfResult { Exam = exam, ImageDetails = imageDetails, Gradings = gradings };
        var examStatus = A.Fake<ExamStatus>();
        examStatus.ExamStatusCodeId = ExamStatusCode.CdiPassedReceived.ExamStatusCodeId;
        var businessRuleStatus = A.Fake<BusinessRuleStatus>();
        businessRuleStatus.IsMet = true;

        A.CallTo(() => _billableRules.IsGradable(A<BillableRuleAnswers>._)).Returns(businessRuleStatus);
        A.CallTo(() => _billableRules.IsBillable(A<BillableRuleAnswers>._)).Returns(businessRuleStatus);
        A.CallTo(() => _mediator.Send(A<CreateStatus>._, A<CancellationToken>._)).Returns(new CreateStatusResponse(null, true));
        A.CallTo(() => _mediator.Send(A<UpdateExamGrade>._, A<CancellationToken>._)).Returns(Unit.Value);
        A.CallTo(() => _mediator.Send(A<GetPdfToClient>._, A<CancellationToken>._)).Returns(pdfToClient);
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>._, A<CancellationToken>._)).Returns(Unit.Value);
        A.CallTo(() => _mediator.Send(A<GetRcmBillId>._, A<CancellationToken>._)).Returns("");
        A.CallTo(() => _mediator.Send(A<GetLatestCdiEvent>._, A<CancellationToken>._)).Returns(examStatus);
        A.CallTo(() => _featureFlags.EnableProviderPayCdi).Returns(true);
        // Act
        await _determineBillabilityOfResultHandler.Handle(message, _messageHandlerContext);

        // Assert
        A.CallTo(() => _billableRules.IsGradable(A<BillableRuleAnswers>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _billableRules.IsBillable(A<BillableRuleAnswers>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreateStatus>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<UpdateExamGrade>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<GetPdfToClient>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<GetRcmBillId>._, A<CancellationToken>._)).MustHaveHappened();
        Assert.IsType<RCMBillingRequestEvent>(_messageHandlerContext.SentMessages[0].Message);
        var sentMessage = (RCMBillingRequestEvent)_messageHandlerContext.SentMessages[0].Message;
        Assert.Equal(exam.EvaluationId, sentMessage.EvaluationId);
        _messageHandlerContext.SentMessages.Length.Should().Be(2);
        var providerPayRequest = _messageHandlerContext.FindSentMessage<ProviderPayRequest>();
        Assert.NotNull(providerPayRequest);
        Assert.Equal(3, providerPayRequest.AdditionalDetails.Count);
        Assert.Equal("12345", providerPayRequest.AdditionalDetails["appointmentId"]);
    }

    [Fact]
    public async Task Should_Process_Payment_Exam_Gradable_And_CdiEvent_Not_Received()
    {
        //// Arrange
        var exam = new ExamModel
        {
            ClientId = 31,
            CreatedDateTime = _applicationTime.UtcNow(),
            DateOfService = _applicationTime.UtcNow(),
            EvaluationId = 45332,
            ExamId = 74632,
            ProviderId = 34567,
            EvaluationObjective = new EvaluationObjective() { EvaluationObjectiveId = 1, Objective = "Comprehensive" }
        };
        var imageDetails = new ResultImageDetails
        {
            LeftEyeOriginalCount = 1,
            RightEyeOriginalCount = 1
        };
        var gradings = new ResultGrading();
        var pdfToClient = new PdfToClientModel { EvaluationId = 45332 };
        var message = new DetermineBillabityOfResult { Exam = exam, ImageDetails = imageDetails, Gradings = gradings };
        var examStatus = A.Fake<ExamStatus>();
        examStatus.ExamStatusCodeId = ExamStatusCode.Performed.ExamStatusCodeId;
        var businessRuleStatus = A.Fake<BusinessRuleStatus>();
        businessRuleStatus.IsMet = true;

        A.CallTo(() => _billableRules.IsGradable(A<BillableRuleAnswers>._)).Returns(businessRuleStatus);
        A.CallTo(() => _billableRules.IsBillable(A<BillableRuleAnswers>._)).Returns(businessRuleStatus);
        A.CallTo(() => _mediator.Send(A<CreateStatus>._, A<CancellationToken>._)).Returns(new CreateStatusResponse(null, true));
        A.CallTo(() => _mediator.Send(A<UpdateExamGrade>._, A<CancellationToken>._)).Returns(Unit.Value);
        A.CallTo(() => _mediator.Send(A<GetPdfToClient>._, A<CancellationToken>._)).Returns(pdfToClient);
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>._, A<CancellationToken>._)).Returns(Unit.Value);
        A.CallTo(() => _mediator.Send(A<GetRcmBillId>._, A<CancellationToken>._)).Returns("");
        A.CallTo(() => _mediator.Send(A<GetLatestCdiEvent>._, A<CancellationToken>._)).Returns(examStatus);
        A.CallTo(() => _featureFlags.EnableProviderPayCdi).Returns(true);
        // Act
        await _determineBillabilityOfResultHandler.Handle(message, _messageHandlerContext);

        // Assert
        A.CallTo(() => _billableRules.IsGradable(A<BillableRuleAnswers>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _billableRules.IsBillable(A<BillableRuleAnswers>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreateStatus>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<UpdateExamGrade>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<GetPdfToClient>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<GetRcmBillId>._, A<CancellationToken>._)).MustHaveHappened();
        Assert.IsType<RCMBillingRequestEvent>(_messageHandlerContext.SentMessages[0].Message);
        var sentMessages = (RCMBillingRequestEvent)_messageHandlerContext.SentMessages[0].Message;
        Assert.Equal(exam.EvaluationId, sentMessages.EvaluationId);
        _messageHandlerContext.SentMessages.Length.Should().Be(1);
        var providerPayRequest = _messageHandlerContext.FindSentMessage<ProviderPayRequest>();
        Assert.Null(providerPayRequest);
    }

    [Fact]
    public async Task Should_Not_Mark_Incomplete_If_One_Eye_Missing_And_Enucleation_Is_True()
    {
        // Arrange
        var exam = new ExamModel
        {
            ClientId = 31,
            CreatedDateTime = _applicationTime.UtcNow(),
            DateOfService = _applicationTime.UtcNow(),
            HasEnucleation = true,
            EvaluationId = 45332,
            ExamId = 74632,
            ProviderId = 34567,
            EvaluationObjective = new EvaluationObjective() { EvaluationObjectiveId = 1, Objective = "Comprehensive" }
        };
        var imageDetails = new ResultImageDetails
        {
            LeftEyeOriginalCount = 0,
            RightEyeOriginalCount = 1
        };
        var gradings = new ResultGrading();
        var pdfToClient = new PdfToClientModel { EvaluationId = 45332 };
        var message = new DetermineBillabityOfResult { Exam = exam, ImageDetails = imageDetails, Gradings = gradings };
        var examStatus = A.Fake<ExamStatus>();
        examStatus.ExamStatusCodeId = ExamStatusCode.Performed.ExamStatusCodeId;
        var businessRuleStatus = A.Fake<BusinessRuleStatus>();
        businessRuleStatus.IsMet = true;

        A.CallTo(() => _billableRules.IsGradable(A<BillableRuleAnswers>._)).Returns(businessRuleStatus);
        A.CallTo(() => _billableRules.IsBillable(A<BillableRuleAnswers>._)).Returns(businessRuleStatus);
        A.CallTo(() => _mediator.Send(A<CreateStatus>._, A<CancellationToken>._)).Returns(new CreateStatusResponse(null, true));
        A.CallTo(() => _mediator.Send(A<UpdateExamGrade>._, A<CancellationToken>._)).Returns(Unit.Value);
        A.CallTo(() => _mediator.Send(A<GetPdfToClient>._, A<CancellationToken>._)).Returns(pdfToClient);
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>._, A<CancellationToken>._)).Returns(Unit.Value);
        A.CallTo(() => _mediator.Send(A<GetRcmBillId>._, A<CancellationToken>._)).Returns("");
        A.CallTo(() => _mediator.Send(A<GetLatestCdiEvent>._, A<CancellationToken>._)).Returns(examStatus);
        A.CallTo(() => _featureFlags.EnableProviderPayCdi).Returns(true);

        // Act
        await _determineBillabilityOfResultHandler.Handle(message, _messageHandlerContext);

        // Assert
        // No Incomplete status creation call should happen because we have enucleation
        A.CallTo(() => _mediator.Send(A<CreateStatus>.That.Matches(s => s.ExamStatusCode.Name == ExamStatusCode.Incomplete.Name), A<CancellationToken>._))
         .MustNotHaveHappened();

        A.CallTo(() => _mediator.Send(A<CreateStatus>.That.Matches(s => s.ExamStatusCode.Name == ExamStatusCode.Gradable.Name), A<CancellationToken>._))
         .MustHaveHappenedOnceExactly();

        _messageHandlerContext.SentMessages.Should().ContainSingle();
        var rcmBillingRequest = (RCMBillingRequestEvent)_messageHandlerContext.SentMessages[0].Message;
        Assert.Equal(45332, rcmBillingRequest.EvaluationId);
    }

    [Fact]
    public async Task Should_Mark_Incomplete_If_One_Eye_Missing_And_Enucleation_Is_False()
    {
        // Arrange
        var exam = new ExamModel
        {
            ClientId = 31,
            CreatedDateTime = _applicationTime.UtcNow(),
            DateOfService = _applicationTime.UtcNow(),
            HasEnucleation = false,
            EvaluationId = 45332,
            ExamId = 74632,
            ProviderId = 34567,
            EvaluationObjective = new EvaluationObjective() { EvaluationObjectiveId = 1, Objective = "Comprehensive" }
        };
        var imageDetails = new ResultImageDetails
        {
            LeftEyeOriginalCount = 0,
            RightEyeOriginalCount = 1
        };
        var gradings = new ResultGrading();
        var pdfToClient = new PdfToClientModel { EvaluationId = 45332 };
        var message = new DetermineBillabityOfResult { Exam = exam, ImageDetails = imageDetails, Gradings = gradings };
        var examStatus = A.Fake<ExamStatus>();
        examStatus.ExamStatusCodeId = ExamStatusCode.Performed.ExamStatusCodeId;
        var businessRuleStatus = A.Fake<BusinessRuleStatus>();
        businessRuleStatus.IsMet = false;

        A.CallTo(() => _billableRules.IsGradable(A<BillableRuleAnswers>._)).Returns(businessRuleStatus);
        A.CallTo(() => _billableRules.IsBillable(A<BillableRuleAnswers>._)).Returns(businessRuleStatus);
        A.CallTo(() => _mediator.Send(A<CreateStatus>._, A<CancellationToken>._)).Returns(new CreateStatusResponse(null, true));
        A.CallTo(() => _mediator.Send(A<UpdateExamGrade>._, A<CancellationToken>._)).Returns(Unit.Value);
        A.CallTo(() => _mediator.Send(A<GetPdfToClient>._, A<CancellationToken>._)).Returns(pdfToClient);
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>._, A<CancellationToken>._)).Returns(Unit.Value);
        A.CallTo(() => _mediator.Send(A<GetRcmBillId>._, A<CancellationToken>._)).Returns("");
        A.CallTo(() => _mediator.Send(A<GetLatestCdiEvent>._, A<CancellationToken>._)).Returns(examStatus);
        A.CallTo(() => _featureFlags.EnableProviderPayCdi).Returns(true);


        // Act
        await _determineBillabilityOfResultHandler.Handle(message, _messageHandlerContext);

        // Assert
        // Should have marked as Incomplete because one eye is missing images and no enucleation
        A.CallTo(() => _mediator.Send(A<CreateStatus>.That.Matches(s => s.ExamStatusCode.Name == ExamStatusCode.Incomplete.Name), A<CancellationToken>._))
         .MustHaveHappenedOnceExactly();
        var rcmBillingEvents = _messageHandlerContext.SentMessages;
        rcmBillingEvents.Length.Should().Be(0, "since Incomplete was set, BillRequest should not be sent");
    }
}