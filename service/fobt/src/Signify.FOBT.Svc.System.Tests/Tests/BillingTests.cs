using FluentResults;
using Newtonsoft.Json.Linq;
using Signify.EvaluationsApi.Core.Values;
using Signify.FOBT.Svc.Core.Constants;
using Signify.FOBT.Svc.Core.Events;
using Signify.FOBT.Svc.System.Tests.Core.Actions;
using Signify.QE.MSTest.Attributes;
using Signify.QE.MSTest.Utilities;
using Signify.Dps.Test.Utilities.DataGen;
using Signify.FOBT.Svc.System.Tests.Core.Constants;
using Signify.FOBT.Svc.System.Tests.Core.Models.Kafka;

namespace Signify.FOBT.Svc.System.Tests.Tests;

[TestClass, TestCategory("regression")]
public class BillingTests : BaseTestActions
{

    [RetryableTestMethod(2)]
    [DataRow("Positive", "A")]
    [DataRow("Negative", "N")]
    public async Task ANC_T346_BillRequestSent(string labresult, string normality)
    {
        // Arrange
        var (member, appointment, evaluation) =
            await CoreApiActions.PrepareEvaluation();
        var answersDict = GeneratePerformedAnswers();

        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId,
            CoreApiActions.GetEvaluationAnswerList(answersDict));
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        
        // Assert
        // Database FOBT table
        var fobt = await GetFOBTByEvaluationId(evaluation.EvaluationId, 20, 5);
        
        var  orderCorrelationId= fobt.OrderCorrelationId;
        var examId = fobt.FOBTId;
        
        // Publish PdfDeliveredToClient event to pdfdelivery kafka topic
        var pdfDeliveryEvent = new PdfDeliveredToClient()
        {
            BatchId = 12345,
            BatchName = "FOBT_System_Tests" + DateTime.Now.Date.ToString("yyyyMMdd"),
            ProductCodes = new List<string>{"FOBT"},
            CreatedDateTime = DateTime.Now,
            DeliveryDateTime = DateTime.Now,
            EvaluationId = evaluation.EvaluationId,
            EventId = DataGen.NewGuid()
        };
        CoreKafkaActions.PublishPdfDeliveryEvent(pdfDeliveryEvent,evaluation.EvaluationId.ToString());
        await Task.Delay(5000);
        
        // Validate that the FOBT details are as expected using EvaluationId in FOBT and FOBTBilling
        var billingResult = await getBillingResultsByFOBTId(examId, 20, 5);
        billingResult[0].BillId.Should().NotBeEmpty();
        billingResult[0].BillingProductCode.Should().Be("FOBT-Left");
        var billId = billingResult[0].BillId; 
        
        // Validate BillRequestSent Message for 'FOBT-Left' Event published in Kafka
        var billRequestSentEvent = await CoreKafkaActions.GetBillRequestSentEvent<BillRequestSentEvent>(evaluation.EvaluationId);
        billRequestSentEvent.BillId.Should().Be(billId);
        billRequestSentEvent.BillingProductCode.Should().Be("FOBT-Left");
        billRequestSentEvent.ProductCode.Should().Be("FOBT");
        billRequestSentEvent.EvaluationId.Should().Be(evaluation.EvaluationId);
        billRequestSentEvent.MemberPlanId.ToString().Should().Be(member.MemberPlanId.ToString());
        billRequestSentEvent.ProviderId.Should().Be(Provider.ProviderId);
        billRequestSentEvent.CreatedDate.Date.Should().Be(pdfDeliveryEvent.CreatedDateTime.Date);
        billRequestSentEvent.PdfDeliveryDate.Date.Should().Be(pdfDeliveryEvent.DeliveryDateTime.Date);
        billRequestSentEvent.ReceivedDate.Date.Should().Be(pdfDeliveryEvent.CreatedDateTime.Date);
        
        // Publish the homeaccess lab results
        var resultsReceivedValue = new HomeAccessLabResults()
        {
            EventId = DataGen.NewUuid(),
            CreatedDateTime= DateTime.Now,
            OrderCorrelationId = orderCorrelationId,
            Barcode = answersDict[Answers.Barcode],
            LabTestType= "FOBT",
            LabResults = labresult,
            AbnormalIndicator = normality,
            Exception = "",
            CollectionDate = DateTime.Now,
            ServiceDate = DateTime.Now,
            ReleaseDate = DateTime.Now
        };
        CoreHomeAccessKafkaActions.PublishEvent<HomeAccessLabResults>("homeaccess_labresults",resultsReceivedValue,evaluation.EvaluationId.ToString(),"HomeAccessResultsReceived");
        await Task.Delay(5000);
        
    // Validate that the database FOBT details for FOBT-Results are as expected using EvaluationId in FOBT and FOBTBilling
        var resultsBilling = await getBillingResultsByFOBTId(examId, 20, 5);
    
        resultsBilling[1].BillId.Should().NotBeEmpty();
        resultsBilling[1].BillingProductCode.Should().Be("FOBT-Results");
        var billResultId = resultsBilling[1].BillId;
    
    // Validate BillRequestSent Message for 'FOBT-Results' Event published in Kafka
        var billResultsRequestSentEvent = await CoreKafkaActions.GetBillRequestSentEvent<BillRequestSentEvent>(evaluation.EvaluationId,"FOBT-Results");
        
        billResultsRequestSentEvent.BillId.Should().Be(billResultId);
        billResultsRequestSentEvent.BillingProductCode.Should().Be("FOBT-Results");
        billResultsRequestSentEvent.ProductCode.Should().Be("FOBT");
        billResultsRequestSentEvent.EvaluationId.Should().Be(evaluation.EvaluationId);
        billResultsRequestSentEvent.MemberPlanId.ToString().Should().Be(member.MemberPlanId.ToString());
        billResultsRequestSentEvent.ProviderId.Should().Be(Provider.ProviderId);
        billResultsRequestSentEvent.CreatedDate.Date.Should().Be(pdfDeliveryEvent.CreatedDateTime.Date);
        billResultsRequestSentEvent.PdfDeliveryDate.Date.Should().Be(pdfDeliveryEvent.DeliveryDateTime.Date);
        billResultsRequestSentEvent.ReceivedDate.Date.Should().Be(pdfDeliveryEvent.CreatedDateTime.Date); 
        
    // Database Status     
        var expectedIds = new List<int>
        {
            ExamStatusCodes.ExamPerformed.FOBTStatusCodeId,
            ExamStatusCodes.LabOrderCreated.FOBTStatusCodeId,
            ExamStatusCodes.FOBTLeft.FOBTStatusCodeId,
            ExamStatusCodes.FOBTResults.FOBTStatusCodeId
        };
        await ValidateExamStatusCodesByEvaluationId(evaluation.EvaluationId, expectedIds, 10, 3);
    }
    
    [RetryableTestMethod]
    public async Task ANC_T349_BillRequestNotSent()
    {
        // Arrange
        var (member, appointment, evaluation) =
            await CoreApiActions.PrepareEvaluation();
        var answersDict = GeneratePerformedAnswers();

        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId,
            CoreApiActions.GetEvaluationAnswerList(answersDict));
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        
        // Assert
        // Database FOBT table
        var fobt = await GetFOBTByEvaluationId(evaluation.EvaluationId, 20, 5);
        
        var  orderCorrelationId= fobt.OrderCorrelationId;
        var examId = fobt.FOBTId;
        
        // Publish PdfDeliveredToClient event to pdfdelivery kafka topic
        var pdfDeliveryEvent = new PdfDeliveredToClient()
        {
            BatchId = 12345,
            BatchName = "FOBT_System_Tests" + DateTime.Now.Date.ToString("yyyyMMdd"),
            ProductCodes = new List<string>{"FOBT"},
            CreatedDateTime = DateTime.Now,
            DeliveryDateTime = DateTime.Now,
            EvaluationId = evaluation.EvaluationId,
            EventId = DataGen.NewGuid()
        };
        CoreKafkaActions.PublishPdfDeliveryEvent(pdfDeliveryEvent,evaluation.EvaluationId.ToString());
        await Task.Delay(5000);
        
        
        // Publish the homeaccess lab results
        var resultsReceivedValue = new HomeAccessLabResults()
        {
            EventId = DataGen.NewUuid(),
            CreatedDateTime= DateTime.Now,
            OrderCorrelationId = orderCorrelationId,
            Barcode = answersDict[Answers.Barcode],
            LabTestType= "FOBT",
            LabResults = "Negative",
            AbnormalIndicator = "U",
            Exception = "Results Expired",
            CollectionDate = DateTime.Now,
            ServiceDate = DateTime.Now,
            ReleaseDate = DateTime.Now
        };
        CoreHomeAccessKafkaActions.PublishEvent<HomeAccessLabResults>("homeaccess_labresults",resultsReceivedValue,evaluation.EvaluationId.ToString(),"HomeAccessResultsReceived");
        await Task.Delay(5000);
        
    
        // Validate that the FOBT details are as expected using EvaluationId in FOBT and FOBTBilling
        var billingResult = await getBillingResultsByFOBTId(examId, 20, 5);
        billingResult[0].BillId.Should().NotBeEmpty();
        billingResult[0].BillingProductCode.Should().Be("FOBT-Left");
        var billId = billingResult[0].BillId; 
        
        // Validate BillRequestSent Message for 'FOBT-Left' Event published in Kafka
        var billRequestSentEvent = await CoreKafkaActions.GetBillRequestSentEvent<BillRequestSentEvent>(evaluation.EvaluationId);
        billRequestSentEvent.BillId.Should().Be(billId);
        billRequestSentEvent.BillingProductCode.Should().Be("FOBT-Left");
        billRequestSentEvent.ProductCode.Should().Be("FOBT");
        billRequestSentEvent.EvaluationId.Should().Be(evaluation.EvaluationId);
        billRequestSentEvent.MemberPlanId.ToString().Should().Be(member.MemberPlanId.ToString());
        billRequestSentEvent.ProviderId.Should().Be(Provider.ProviderId);
        billRequestSentEvent.CreatedDate.Date.Should().Be(pdfDeliveryEvent.CreatedDateTime.Date);
        billRequestSentEvent.PdfDeliveryDate.Date.Should().Be(pdfDeliveryEvent.DeliveryDateTime.Date);
        billRequestSentEvent.ReceivedDate.Date.Should().Be(pdfDeliveryEvent.CreatedDateTime.Date);
    
       // Validate BillRequestSent Message for 'FOBT-Results' Event published in Kafka
        var billRequestNotSentEvent = await CoreKafkaActions.GetBillRequestNotSentEvent<BillRequestNotSent>(evaluation.EvaluationId);
        
        billRequestNotSentEvent.BillingProductCode.Should().Be("FOBT-Results");
        billRequestNotSentEvent.ProductCode.Should().Be("FOBT");
        billRequestNotSentEvent.EvaluationId.Should().Be(evaluation.EvaluationId);
        billRequestNotSentEvent.MemberPlanId.ToString().Should().Be(member.MemberPlanId.ToString());
        billRequestNotSentEvent.ProviderId.Should().Be(Provider.ProviderId);
        billRequestNotSentEvent.CreatedDate.Date.Should().Be(pdfDeliveryEvent.CreatedDateTime.Date);
        billRequestNotSentEvent.ReceivedDate.Date.Should().Be(pdfDeliveryEvent.CreatedDateTime.Date); 
        
      // Database Status     
        var expectedIds = new List<int>
        {
            ExamStatusCodes.ExamPerformed.FOBTStatusCodeId,
            ExamStatusCodes.LabOrderCreated.FOBTStatusCodeId,
            ExamStatusCodes.FOBTLeft.FOBTStatusCodeId,
            ExamStatusCodes.InvalidLabResultsReceived.FOBTStatusCodeId,
            ExamStatusCodes.BillRequestNotSent.FOBTStatusCodeId
        };
        await ValidateExamStatusCodesByEvaluationId(evaluation.EvaluationId, expectedIds, 10, 3);
    }
}