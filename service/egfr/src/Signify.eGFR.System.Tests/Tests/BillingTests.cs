using Signify.Dps.Test.Utilities.DataGen;
using Signify.eGFR.System.Tests.Core.Actions;
using Signify.EvaluationsApi.Core.Values;
using Signify.QE.MSTest.Attributes;
using Signify.eGFR.System.Tests.Core.Constants;
using Signify.eGFR.Core.Events.Akka;
using Signify.eGFR.System.Tests.Core.Models.Kafka;

namespace Signify.eGFR.System.Tests.Tests;

[TestClass, TestCategory("regression")]
public class BillingTests : BaseTestActions
{
    public TestContext TestContext { get; set; }
    
    [RetryableTestMethod(2)]
    [DataRow("65", "N")]
    [DataRow("59", "A")]
    public async Task ANC_T552_BillRequestSent(string egfrResult, string normality)
    
    {
         // Arrange
         var (member, appointment, evaluation) =
             await CoreApiActions.PrepareEvaluation();
         var answersDict = GenerateKedPerformedAnswers();
         // Act
         CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId,
             CoreApiActions.GetEvaluationAnswerList(answersDict));
         CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
         CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);

         // Assert
         // Database eGFR table
         var egfr = await GetExamByEvaluationId(evaluation.EvaluationId);
         var examId = egfr.ExamId;
         
         egfr.EvaluationId.Should().Be(Convert.ToInt32(evaluation.EvaluationId));
         egfr.MemberPlanId.Should().Be(Convert.ToInt32(member.MemberPlanId));
         egfr.MemberId.Should().Be(Convert.ToInt32(member.MemberId));
         egfr.CenseoId.Should().Be(member.CenseoId);
         egfr.AppointmentId.Should().Be(appointment.AppointmentId);
         egfr.ProviderId.Should().Be(Provider.ProviderId);
         egfr.DateOfService.Should().Be(DateTime.Parse(answersDict[Answers.DosAnswerId]).Date);
         egfr.ClientId.Should().Be(member.ClientID);
         egfr.City.Should().Be(member.City);
         egfr.State.Should().Be(member.State);
         egfr.AddressLineOne.Should().Be(member.AddressLineOne);
         egfr.AddressLineTwo.Should().Be(member.AddressLineTwo);
         egfr.ZipCode.Should().Be(member.ZipCode);
         egfr.NationalProviderIdentifier.Should().Be(Provider.NationalProviderIdentifier);
         egfr.FirstName.Should().Be(member.FirstName);
         egfr.LastName.Should().Be(member.LastName);
         egfr.MiddleName.Should().Be(member.MiddleName);
         egfr.DateOfBirth.Should().Be(member.DateOfBirth);
         egfr.EvaluationReceivedDateTime.Should().BeCloseTo(evaluation.ReceivedDateTime, TimeSpan.FromSeconds(10));
         
         //Validate Exam Status Update in database
         
         var expectedIds = new List<int>
         {
             ExamStatusCodes.ExamPerformed.ExamStatusCodeId
         };

         var finalizedEvent =
             await CoreKafkaActions.GetEvaluationEvent<EvaluationFinalizedEvent>(evaluation.EvaluationId,
                 "EvaluationFinalizedEvent");
         await ValidateExamStatusCodesByEvaluationId(evaluation.EvaluationId, expectedIds);
         
         // Publish the homeaccess lab results
         var resultsReceivedValue = new HomeAccessLabResults()
         {
             EstimatedGlomerularFiltrationRateResultColor = "",
             EstimatedGlomerularFiltrationRateResultDescription = "abcd",
             EgfrResult = egfrResult,
             EvaluationId = evaluation.EvaluationId,
             DateLabReceived = DateTime.UtcNow,
         };
         CoreHomeAccessKafkaActions.PublishEgfrLabResultEvent(resultsReceivedValue,evaluation.EvaluationId.ToString());
         await Task.Delay(5000);
         
         var exam = await GetExamByEvaluationId(evaluation.EvaluationId);
         
         var pdfDeliveryEvent = new PdfDeliveredToClientEvent()
         {
             BatchId = 2,
             BatchName = "Ancillary_Services_Karate_Tests_" + DateTime.Now.Date.ToString("yyyyMMdd"),
             ProductCodes = new List<string>{"EGFR"},
             CreatedDateTime = DateTime.Now,
             DeliveryDateTime = DateTime.Now,
             EvaluationId = evaluation.EvaluationId,
             EventId = DataGen.NewGuid()
         };
        
         // Publish PdfDeliveredToClient event to pdfdelivery kafka topic
         CoreKafkaActions.PublishPdfDeliveryEvent(pdfDeliveryEvent,evaluation.EvaluationId.ToString());
         await Task.Delay(5000);
         
         // Validate record present in table PdfDeliveredToClient in database
         var pdfDelivered = await GetPdfDeliveredByEvaluationId(evaluation.EvaluationId);
         pdfDelivered.BatchId.Should().Be(pdfDeliveryEvent.BatchId);
         pdfDelivered.BatchName.Should().Be(pdfDeliveryEvent.BatchName);
         pdfDelivered.EventId.Should().Be(pdfDeliveryEvent.EventId);
         
         // Validate the BillRequestSent table in database for BillRequestSent using ExamId
         var resultsBilling = await GetBillRequestByExamId(examId);
         resultsBilling[0].BillId.Should().NotBeEmpty();
         resultsBilling[0].BillingProductCode.Should().Be("eGFR");
         var billId= resultsBilling[0].BillId;
    
         // Validate BillRequestSent Message for 'egfr_status' Event published in Kafka
         var billResultsRequestSentEvent = await CoreKafkaActions.GetEgfrBillRequestSentEvent<BillRequestSentEvent>(evaluation.EvaluationId);
        
         billResultsRequestSentEvent.BillId.Should().Be(billId);
         billResultsRequestSentEvent.BillingProductCode.Should().Be("EGFR");
         billResultsRequestSentEvent.ProductCode.Should().Be("EGFR");
         billResultsRequestSentEvent.EvaluationId.Should().Be(evaluation.EvaluationId);
         billResultsRequestSentEvent.MemberPlanId.ToString().Should().Be(member.MemberPlanId.ToString());
         billResultsRequestSentEvent.ProviderId.Should().Be(Provider.ProviderId);
         billResultsRequestSentEvent.CreatedDate.Date.Should().Be(pdfDeliveryEvent.CreatedDateTime.Date);
         billResultsRequestSentEvent.PdfDeliveryDate.Date.Should().Be(pdfDeliveryEvent.DeliveryDateTime.Date);
         billResultsRequestSentEvent.ReceivedDate.Date.Should().Be(pdfDeliveryEvent.CreatedDateTime.Date); 
     }
    
    
    [RetryableTestMethod(2)]
    [DataRow("0", "U")]
    public async Task ANC_T593_BillRequestNotSent(string egfrResult, string normality)
    
    {
         // Arrange
         var (member, appointment, evaluation) =
             await CoreApiActions.PrepareEvaluation();
         var answersDict = GenerateKedPerformedAnswers();
         // Act
         CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId,
             CoreApiActions.GetEvaluationAnswerList(answersDict));
         CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
         CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);

         // Assert
         // Database eGFR table
         var egfr = await GetExamByEvaluationId(evaluation.EvaluationId);
         var examId = egfr.ExamId;
         
         egfr.EvaluationId.Should().Be(Convert.ToInt32(evaluation.EvaluationId));
         egfr.MemberPlanId.Should().Be(Convert.ToInt32(member.MemberPlanId));
         egfr.MemberId.Should().Be(Convert.ToInt32(member.MemberId));
         egfr.CenseoId.Should().Be(member.CenseoId);
         egfr.AppointmentId.Should().Be(appointment.AppointmentId);
         egfr.ProviderId.Should().Be(Provider.ProviderId);
         egfr.DateOfService.Should().Be(DateTime.Parse(answersDict[Answers.DosAnswerId]).Date);
         egfr.ClientId.Should().Be(member.ClientID);
         egfr.City.Should().Be(member.City);
         egfr.State.Should().Be(member.State);
         egfr.AddressLineOne.Should().Be(member.AddressLineOne);
         egfr.AddressLineTwo.Should().Be(member.AddressLineTwo);
         egfr.ZipCode.Should().Be(member.ZipCode);
         egfr.NationalProviderIdentifier.Should().Be(Provider.NationalProviderIdentifier);
         egfr.FirstName.Should().Be(member.FirstName);
         egfr.LastName.Should().Be(member.LastName);
         egfr.MiddleName.Should().Be(member.MiddleName);
         egfr.DateOfBirth.Should().Be(member.DateOfBirth);
         egfr.EvaluationReceivedDateTime.Should().BeCloseTo(evaluation.ReceivedDateTime, TimeSpan.FromSeconds(10));
         
         //Validate Exam Status Update in database
         
         var expectedIds = new List<int>
         {
             ExamStatusCodes.ExamPerformed.ExamStatusCodeId
         };

         var finalizedEvent =
             await CoreKafkaActions.GetEvaluationEvent<EvaluationFinalizedEvent>(evaluation.EvaluationId,
                 "EvaluationFinalizedEvent");
         await ValidateExamStatusCodesByEvaluationId(evaluation.EvaluationId, expectedIds);
         
         // Publish the homeaccess lab results
         var resultsReceivedValue = new HomeAccessLabResults()
         {
             EstimatedGlomerularFiltrationRateResultColor = "",
             EstimatedGlomerularFiltrationRateResultDescription = "abcd",
             EgfrResult = egfrResult,
             EvaluationId = evaluation.EvaluationId,
             DateLabReceived = DateTime.UtcNow,
         };
         CoreHomeAccessKafkaActions.PublishEgfrLabResultEvent(resultsReceivedValue,evaluation.EvaluationId.ToString());
         await Task.Delay(5000);
         
         var exam = await GetExamByEvaluationId(evaluation.EvaluationId);
         
         var pdfDeliveryEvent = new PdfDeliveredToClientEvent()
         {
             BatchId = 2,
             BatchName = "Ancillary_Services_Karate_Tests_" + DateTime.Now.Date.ToString("yyyyMMdd"),
             ProductCodes = new List<string>{"EGFR"},
             CreatedDateTime = DateTime.Now,
             DeliveryDateTime = DateTime.Now,
             EvaluationId = evaluation.EvaluationId,
             EventId = DataGen.NewGuid()
         };
        
         // Publish PdfDeliveredToClient event to pdfdelivery kafka topic
         CoreKafkaActions.PublishPdfDeliveryEvent(pdfDeliveryEvent,evaluation.EvaluationId.ToString());
         await Task.Delay(5000);
         
         // Validate record present in table PdfDeliveredToClient in database
         var pdfDelivered = await GetPdfDeliveredByEvaluationId(evaluation.EvaluationId);
         pdfDelivered.BatchId.Should().Be(pdfDeliveryEvent.BatchId);
         pdfDelivered.BatchName.Should().Be(pdfDeliveryEvent.BatchName);
         pdfDelivered.EventId.Should().Be(pdfDeliveryEvent.EventId);
         
         // Validate the BillRequestSent table in database for BillRequestSent using ExamId
         // var resultsBilling = await GetBillRequestByExamId(examId);
         // resultsBilling[0].BillId = null ;
    
         // Validate BillRequestNotSent Message for 'egfr_status' Event published in Kafka
         var billResultsRequestNotSentEvent = await CoreKafkaActions.GetEgfrBillRequestNotSentEvent<BillRequestNotSentEvent>(evaluation.EvaluationId);
         
         billResultsRequestNotSentEvent.BillingProductCode.Should().Be("EGFR");
         billResultsRequestNotSentEvent.ProductCode.Should().Be("EGFR");
         billResultsRequestNotSentEvent.EvaluationId.Should().Be(evaluation.EvaluationId);
         billResultsRequestNotSentEvent.MemberPlanId.ToString().Should().Be(member.MemberPlanId.ToString());
         billResultsRequestNotSentEvent.ProviderId.Should().Be(Provider.ProviderId);
         billResultsRequestNotSentEvent.CreatedDate.Date.Should().Be(pdfDeliveryEvent.CreatedDateTime.Date);
         billResultsRequestNotSentEvent.PdfDeliveryDate.Date.Should().Be(pdfDeliveryEvent.DeliveryDateTime.Date);
         billResultsRequestNotSentEvent.ReceivedDate.Date.Should().Be(pdfDeliveryEvent.CreatedDateTime.Date); 
     }
    
    [RetryableTestMethod(2)]
    [DataRow("65", "N")]
    [DataRow("59", "A")]
    
    public async Task ANC_T595_eGFR_normal_BillRequestSent_pdf_delivered_before_result_received(string egfrResult, string normality)
    
    {
         // Arrange
         var (member, appointment, evaluation) =
             await CoreApiActions.PrepareEvaluation();
         var answersDict = GenerateKedPerformedAnswers();
         // Act
         CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId,
             CoreApiActions.GetEvaluationAnswerList(answersDict));
         CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
         CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);

         // Assert
         // Database eGFR table
         var egfr = await GetExamByEvaluationId(evaluation.EvaluationId);
         var examId = egfr.ExamId;
         
         egfr.EvaluationId.Should().Be(Convert.ToInt32(evaluation.EvaluationId));
         egfr.MemberPlanId.Should().Be(Convert.ToInt32(member.MemberPlanId));
         egfr.MemberId.Should().Be(Convert.ToInt32(member.MemberId));
         egfr.CenseoId.Should().Be(member.CenseoId);
         egfr.AppointmentId.Should().Be(appointment.AppointmentId);
         egfr.ProviderId.Should().Be(Provider.ProviderId);
         egfr.DateOfService.Should().Be(DateTime.Parse(answersDict[Answers.DosAnswerId]).Date);
         egfr.ClientId.Should().Be(member.ClientID);
         egfr.City.Should().Be(member.City);
         egfr.State.Should().Be(member.State);
         egfr.AddressLineOne.Should().Be(member.AddressLineOne);
         egfr.AddressLineTwo.Should().Be(member.AddressLineTwo);
         egfr.ZipCode.Should().Be(member.ZipCode);
         egfr.NationalProviderIdentifier.Should().Be(Provider.NationalProviderIdentifier);
         egfr.FirstName.Should().Be(member.FirstName);
         egfr.LastName.Should().Be(member.LastName);
         egfr.MiddleName.Should().Be(member.MiddleName);
         egfr.DateOfBirth.Should().Be(member.DateOfBirth);
         egfr.EvaluationReceivedDateTime.Should().BeCloseTo(evaluation.ReceivedDateTime, TimeSpan.FromSeconds(10));
         
         //Validate Exam Status Update in database
         
         var expectedIds = new List<int>
         {
             ExamStatusCodes.ExamPerformed.ExamStatusCodeId
         };

         var finalizedEvent =
             await CoreKafkaActions.GetEvaluationEvent<EvaluationFinalizedEvent>(evaluation.EvaluationId,
                 "EvaluationFinalizedEvent");
         await ValidateExamStatusCodesByEvaluationId(evaluation.EvaluationId, expectedIds);
         
         var pdfDeliveryEvent = new PdfDeliveredToClientEvent()
         {
             BatchId = 2,
             BatchName = "Ancillary_Services_Karate_Tests_" + DateTime.Now.Date.ToString("yyyyMMdd"),
             ProductCodes = new List<string>{"EGFR"},
             CreatedDateTime = DateTime.Now,
             DeliveryDateTime = DateTime.Now,
             EvaluationId = evaluation.EvaluationId,
             EventId = DataGen.NewGuid()
         };
        
         // Publish PdfDeliveredToClient event to pdfdelivery kafka topic
         CoreKafkaActions.PublishPdfDeliveryEvent(pdfDeliveryEvent,evaluation.EvaluationId.ToString());
         await Task.Delay(5000);
         
         // Validate record present in table PdfDeliveredToClient in database
         var pdfDelivered = await GetPdfDeliveredByEvaluationId(evaluation.EvaluationId);
         pdfDelivered.BatchId.Should().Be(pdfDeliveryEvent.BatchId);
         pdfDelivered.BatchName.Should().Be(pdfDeliveryEvent.BatchName);
         pdfDelivered.EventId.Should().Be(pdfDeliveryEvent.EventId);
         
         // Publish the homeaccess lab results
         var resultsReceivedValue = new HomeAccessLabResults()
         {
             EstimatedGlomerularFiltrationRateResultColor = "",
             EstimatedGlomerularFiltrationRateResultDescription = "abcd",
             EgfrResult = egfrResult,
             EvaluationId = evaluation.EvaluationId,
             DateLabReceived = DateTime.UtcNow,
         };
         CoreHomeAccessKafkaActions.PublishEgfrLabResultEvent(resultsReceivedValue,evaluation.EvaluationId.ToString());
         await Task.Delay(5000);
         
         var exam = await GetExamByEvaluationId(evaluation.EvaluationId);
         
         // Validate the BillRequestSent table in database for BillRequestSent using ExamId
         var resultsBilling = await GetBillRequestByExamId(examId);
         resultsBilling[0].BillId.Should().NotBeEmpty();
         resultsBilling[0].BillingProductCode.Should().Be("eGFR");
         var billId= resultsBilling[0].BillId;
    
         // Validate BillRequestSent Message for 'egfr_status' Event published in Kafka
         var billResultsRequestSentEvent = await CoreKafkaActions.GetEgfrBillRequestSentEvent<BillRequestSentEvent>(evaluation.EvaluationId);
        
         billResultsRequestSentEvent.BillId.Should().Be(billId);
         billResultsRequestSentEvent.BillingProductCode.Should().Be("EGFR");
         billResultsRequestSentEvent.ProductCode.Should().Be("EGFR");
         billResultsRequestSentEvent.EvaluationId.Should().Be(evaluation.EvaluationId);
         billResultsRequestSentEvent.MemberPlanId.ToString().Should().Be(member.MemberPlanId.ToString());
         billResultsRequestSentEvent.ProviderId.Should().Be(Provider.ProviderId);
         billResultsRequestSentEvent.CreatedDate.Date.Should().Be(pdfDeliveryEvent.CreatedDateTime.Date);
         billResultsRequestSentEvent.PdfDeliveryDate.Date.Should().Be(pdfDeliveryEvent.DeliveryDateTime.Date);
         billResultsRequestSentEvent.ReceivedDate.Date.Should().Be(pdfDeliveryEvent.CreatedDateTime.Date); 
     }
    
    [RetryableTestMethod(2)]
    [DataRow("0", "U")]
    public async Task ANC_T596_eGFR_abnormal_BillRequestNotSent_pdf_delivered_before_results(string egfrResult, string normality)
    
    {
         // Arrange
         var (member, appointment, evaluation) =
             await CoreApiActions.PrepareEvaluation();
         var answersDict = GenerateKedPerformedAnswers();
         // Act
         CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId,
             CoreApiActions.GetEvaluationAnswerList(answersDict));
         CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
         CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);

         // Assert
         // Database eGFR table
         var egfr = await GetExamByEvaluationId(evaluation.EvaluationId);
         var examId = egfr.ExamId;
         
         egfr.EvaluationId.Should().Be(Convert.ToInt32(evaluation.EvaluationId));
         egfr.MemberPlanId.Should().Be(Convert.ToInt32(member.MemberPlanId));
         egfr.MemberId.Should().Be(Convert.ToInt32(member.MemberId));
         egfr.CenseoId.Should().Be(member.CenseoId);
         egfr.AppointmentId.Should().Be(appointment.AppointmentId);
         egfr.ProviderId.Should().Be(Provider.ProviderId);
         egfr.DateOfService.Should().Be(DateTime.Parse(answersDict[Answers.DosAnswerId]).Date);
         egfr.ClientId.Should().Be(member.ClientID);
         egfr.City.Should().Be(member.City);
         egfr.State.Should().Be(member.State);
         egfr.AddressLineOne.Should().Be(member.AddressLineOne);
         egfr.AddressLineTwo.Should().Be(member.AddressLineTwo);
         egfr.ZipCode.Should().Be(member.ZipCode);
         egfr.NationalProviderIdentifier.Should().Be(Provider.NationalProviderIdentifier);
         egfr.FirstName.Should().Be(member.FirstName);
         egfr.LastName.Should().Be(member.LastName);
         egfr.MiddleName.Should().Be(member.MiddleName);
         egfr.DateOfBirth.Should().Be(member.DateOfBirth);
         egfr.EvaluationReceivedDateTime.Should().BeCloseTo(evaluation.ReceivedDateTime, TimeSpan.FromSeconds(10));
         
         //Validate Exam Status Update in database
         
         var expectedIds = new List<int>
         {
             ExamStatusCodes.ExamPerformed.ExamStatusCodeId
         };

         var finalizedEvent =
             await CoreKafkaActions.GetEvaluationEvent<EvaluationFinalizedEvent>(evaluation.EvaluationId,
                 "EvaluationFinalizedEvent");
         await ValidateExamStatusCodesByEvaluationId(evaluation.EvaluationId, expectedIds);
         
         var pdfDeliveryEvent = new PdfDeliveredToClientEvent()
         {
             BatchId = 2,
             BatchName = "Ancillary_Services_Karate_Tests_" + DateTime.Now.Date.ToString("yyyyMMdd"),
             ProductCodes = new List<string>{"EGFR"},
             CreatedDateTime = DateTime.Now,
             DeliveryDateTime = DateTime.Now,
             EvaluationId = evaluation.EvaluationId,
             EventId = DataGen.NewGuid()
         };
        
         // Publish PdfDeliveredToClient event to pdfdelivery kafka topic
         CoreKafkaActions.PublishPdfDeliveryEvent(pdfDeliveryEvent,evaluation.EvaluationId.ToString());
         await Task.Delay(5000);
         
         // Validate record present in table PdfDeliveredToClient in database
         var pdfDelivered = await GetPdfDeliveredByEvaluationId(evaluation.EvaluationId);
         pdfDelivered.BatchId.Should().Be(pdfDeliveryEvent.BatchId);
         pdfDelivered.BatchName.Should().Be(pdfDeliveryEvent.BatchName);
         pdfDelivered.EventId.Should().Be(pdfDeliveryEvent.EventId);
         
         // Publish the homeaccess lab results
         var resultsReceivedValue = new HomeAccessLabResults()
         {
             EstimatedGlomerularFiltrationRateResultColor = "",
             EstimatedGlomerularFiltrationRateResultDescription = "abcd",
             EgfrResult = egfrResult,
             EvaluationId = evaluation.EvaluationId,
             DateLabReceived = DateTime.UtcNow,
         };
         CoreHomeAccessKafkaActions.PublishEgfrLabResultEvent(resultsReceivedValue,evaluation.EvaluationId.ToString());
         await Task.Delay(5000);
         
         var exam = await GetExamByEvaluationId(evaluation.EvaluationId);
         
         // Validate the BillRequestSent table in database for BillRequestSent using ExamId
         // var resultsBilling = await GetBillRequestByExamId(examId, 20, 5);
         // resultsBilling[0].BillId = null ;
    
         // Validate BillRequestNotSent Message for 'egfr_status' Event published in Kafka
         var billResultsRequestNotSentEvent = await CoreKafkaActions.GetEgfrBillRequestNotSentEvent<BillRequestNotSentEvent>(evaluation.EvaluationId);
         
         billResultsRequestNotSentEvent.BillingProductCode.Should().Be("EGFR");
         billResultsRequestNotSentEvent.ProductCode.Should().Be("EGFR");
         billResultsRequestNotSentEvent.EvaluationId.Should().Be(evaluation.EvaluationId);
         billResultsRequestNotSentEvent.MemberPlanId.ToString().Should().Be(member.MemberPlanId.ToString());
         billResultsRequestNotSentEvent.ProviderId.Should().Be(Provider.ProviderId);
         billResultsRequestNotSentEvent.CreatedDate.Date.Should().Be(pdfDeliveryEvent.CreatedDateTime.Date);
         billResultsRequestNotSentEvent.PdfDeliveryDate.Date.Should().Be(pdfDeliveryEvent.DeliveryDateTime.Date);
         billResultsRequestNotSentEvent.ReceivedDate.Date.Should().Be(pdfDeliveryEvent.CreatedDateTime.Date); 
     }
}