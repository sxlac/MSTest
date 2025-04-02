using Signify.EvaluationsApi.Core.Values;
using Signify.Spirometry.Svc.System.Tests.Core.Constants;
using Signify.Spirometry.Svc.System.Tests.Core.Actions;
using Signify.Spirometry.Svc.System.Tests.Core.Models.Kafka;
using Signify.QE.MSTest.Attributes;
using Signify.Dps.Test.Utilities.DataGen;
using Signify.Spirometry.Svc.System.Tests.Core.Exceptions;
using SpiroEvents;
using BillRequestNotSent = Signify.Spirometry.Core.Events.Status.BillRequestNotSent;

namespace Signify.Spirometry.Svc.System.Tests.Tests;

[TestClass, TestCategory("regression")]
public class SpiroBillingTests : PerformedActions
{
    [RetryableTestMethod]
    [DynamicData(nameof(GetSpiroTestData))]
    public async Task ANC_T707_BillRequestSent_EvaluationFinalizedBeforePDFDelivery(Dictionary<int, string> answersDict)
    {
        // Arrange
        var (member, appointment, evaluation) = await CoreApiActions.PrepareEvaluation();
        
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId, CoreApiActions.GetEvaluationAnswerList(answersDict));
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        
        // Publish the PDF event to the pdfdelivery topic
        var pdfDeliveryEvent = new PdfDeliveredToClient()
        {
            BatchId = 12345,
            BatchName = "Spiro_System_Tests" + DateTime.Now.Date.ToString("yyyyMMdd"),
            ProductCodes = new List<string>{"Spirometry"},
            CreatedDateTime = DateTime.Now,
            DeliveryDateTime = DateTime.Now,
            EvaluationId = evaluation.EvaluationId,
            EventId = DataGen.NewGuid()
        };
        CoreKafkaActions.PublishPdfDeliveryEvent(pdfDeliveryEvent,evaluation.EvaluationId.ToString());
        await Task.Delay(5000);
        
        //  Validate the entry in the BillRequestSent table
        var spiro = await getBillingResultByEvaluationId(evaluation.EvaluationId);
        Assert.AreNotEqual(Guid.Empty, spiro.BillId);
        
        // Validate Kafka BillRequestSent event
        var billRequestSentEvent = await CoreKafkaActions.GetSpiroBillRequestSentEvent<BillRequestSentEvent>(evaluation.EvaluationId);
        
        Assert.AreEqual(spiro.BillId, billRequestSentEvent.BillId);
        Assert.AreEqual("SPIROMETRY", billRequestSentEvent.BillingProductCode);
        Assert.AreEqual("SPIROMETRY", billRequestSentEvent.ProductCode);
        Assert.AreEqual(evaluation.EvaluationId, billRequestSentEvent.EvaluationId);
        Assert.AreEqual(member.MemberPlanId.ToString(), billRequestSentEvent.MemberPlanId.ToString());
        Assert.AreEqual(Provider.ProviderId, billRequestSentEvent.ProviderId);
        Assert.AreEqual(pdfDeliveryEvent.CreatedDateTime.Date, billRequestSentEvent.CreateDate.Date);
        Assert.AreEqual(pdfDeliveryEvent.DeliveryDateTime.Date, billRequestSentEvent.PdfDeliveryDate.Date);
        Assert.AreEqual(pdfDeliveryEvent.CreatedDateTime.Date, billRequestSentEvent.ReceivedDate.Date);
        
        // Validate Status Codes in Database
        var expectedIds = new List<int>
        {
            ExamStatusCodes.ExamPerformed.SpiroStatusCodeId,
            ExamStatusCodes.ClientPDFDelivered.SpiroStatusCodeId,
            ExamStatusCodes.BillRequestSent.SpiroStatusCodeId,
        };
        await ValidateExamStatusCodesByExamId(spiro.SpirometryExamId, expectedIds, 10, 3);
    }
    
    [RetryableTestMethod]
    [DynamicData(nameof(GetSpiroTestData))]
    public async Task ANC_T708_BillRequestSent_EvaluationFinalizedAfterPDFDelivery(Dictionary<int, string> answersDict)
    {
        // Arrange
        var (member, appointment, evaluation) = await CoreApiActions.PrepareEvaluation();
        
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId, CoreApiActions.GetEvaluationAnswerList(answersDict));
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        
        // Publish the PDF event to the pdfdelivery topic
        var pdfDeliveryEvent = new PdfDeliveredToClient()
        {
            BatchId = 12345,
            BatchName = "Spiro_System_Tests" + DateTime.Now.Date.ToString("yyyyMMdd"),
            ProductCodes = new List<string>{"Spirometry"},
            CreatedDateTime = DateTime.Now,
            DeliveryDateTime = DateTime.Now,
            EvaluationId = evaluation.EvaluationId,
            EventId = DataGen.NewGuid()
        };
        CoreKafkaActions.PublishPdfDeliveryEvent(pdfDeliveryEvent,evaluation.EvaluationId.ToString());
        await Task.Delay(5000);
        
        // Evaluation Finalized
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        await Task.Delay(5000);
        
        //  Validate the entry in the BillRequestSent table
        var spiro = await getBillingResultByEvaluationId(evaluation.EvaluationId);
        Assert.AreNotEqual(Guid.Empty, spiro.BillId);
        
        // Validate Kafka BillRequestSent event
        var billRequestSentEvent = await CoreKafkaActions.GetSpiroBillRequestSentEvent<BillRequestSentEvent>(evaluation.EvaluationId);
        
        Assert.AreEqual(spiro.BillId, billRequestSentEvent.BillId);
        Assert.AreEqual("SPIROMETRY", billRequestSentEvent.BillingProductCode);
        Assert.AreEqual("SPIROMETRY", billRequestSentEvent.ProductCode);
        Assert.AreEqual(evaluation.EvaluationId, billRequestSentEvent.EvaluationId);
        Assert.AreEqual(member.MemberPlanId.ToString(), billRequestSentEvent.MemberPlanId.ToString());
        Assert.AreEqual(Provider.ProviderId, billRequestSentEvent.ProviderId);
        Assert.AreEqual(pdfDeliveryEvent.CreatedDateTime.Date, billRequestSentEvent.CreateDate.Date);
        Assert.AreEqual(pdfDeliveryEvent.DeliveryDateTime.Date, billRequestSentEvent.PdfDeliveryDate.Date);
        Assert.AreEqual(pdfDeliveryEvent.CreatedDateTime.Date, billRequestSentEvent.ReceivedDate.Date);
        
        // Validate Status Codes in Database
        var expectedIds = new List<int>
        {
            ExamStatusCodes.ExamPerformed.SpiroStatusCodeId,
            ExamStatusCodes.ClientPDFDelivered.SpiroStatusCodeId,
            ExamStatusCodes.BillRequestSent.SpiroStatusCodeId,
        };
        await ValidateExamStatusCodesByExamId(spiro.SpirometryExamId, expectedIds, 10, 3);
    }
    
    [RetryableTestMethod]
    [DynamicData(nameof(GetResultsData))]
    public async Task ANC_T709_BillRequestNotSent_ExamPerformedWithUndeterminedNormality(Dictionary<int, string> answersDict)
    {
        // Arrange
        var (member, appointment, evaluation) = await CoreApiActions.PrepareEvaluation();
        
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId, CoreApiActions.GetEvaluationAnswerList(answersDict));
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        
        // Publish the PDF event to the pdfdelivery topic
        var pdfDeliveryEvent = new PdfDeliveredToClient()
        {
            BatchId = 12345,
            BatchName = "Spiro_System_Tests" + DateTime.Now.Date.ToString("yyyyMMdd"),
            ProductCodes = new List<string>{"Spirometry"},
            CreatedDateTime = DateTime.Now,
            DeliveryDateTime = DateTime.Now,
            EvaluationId = evaluation.EvaluationId,
            EventId = DataGen.NewGuid()
        };
        CoreKafkaActions.PublishPdfDeliveryEvent(pdfDeliveryEvent,evaluation.EvaluationId.ToString());
        await Task.Delay(5000);
        
        // Evaluation Finalized
        await Task.Delay(5000);
        
        // Publish the Overread results
        var resultsReceivedValue = new OverreadProcessed()
        {
            Comment = "TestComment",
            BestPefTestComment= "TestComment",
            Fev1FvcRatio = 0.5m,                    //add m suffix to treat the value as decimal
            ObstructionPerOverread = "INCONCLUSIVE",
            BestFvcTestComment= "TestComment",
            BestPefTestId = DataGen.NewGuid(),
            MemberId = member.MemberPlanId,
            BestFev1TestId = DataGen.NewGuid(),
            OverreadId = DataGen.NewGuid(),
            AppointmentId = appointment.AppointmentId,
            OverreadBy = "JohnDoe",
            BestFvcTestId = DataGen.NewGuid(),
            SessionId = DataGen.NewGuid(),
            ReceivedDateTime = DateTime.Now,
            PerformedDateTime = DateTime.Now,
            OverreadDateTime = DateTime.Now
        };
        CoreKafkaActions.PublishEvent<OverreadProcessed>("overread_spirometry",resultsReceivedValue,evaluation.EvaluationId.ToString(),"OverreadProcessed");
        await Task.Delay(5000);
        
        //  Validate the entry in the BillRequestSent table
        var spiro = await getSpiroExamByEvaluationId(evaluation.EvaluationId);
        
        var exception = await Assert.ThrowsExceptionAsync<SpiroNotFoundException>( async () => await getBillingResultByEvaluationId(evaluation.EvaluationId) );
        Assert.AreEqual($"EvaluationId {evaluation.EvaluationId} not found in Spirometry BillRequestSent table", exception.Message);
        
        // Validate Kafka BillRequestNotSent event
        var billRequestNotSentEvent = await CoreKafkaActions.GetSpiroBillRequestNotSentEvent<BillRequestNotSent>(evaluation.EvaluationId);
        
        Assert.AreEqual("SPIROMETRY", billRequestNotSentEvent.ProductCode);
        Assert.AreEqual(evaluation.EvaluationId, billRequestNotSentEvent.EvaluationId);
        Assert.AreEqual(member.MemberPlanId.ToString(), billRequestNotSentEvent.MemberPlanId.ToString());
        Assert.AreEqual(Provider.ProviderId, billRequestNotSentEvent.ProviderId);
        Assert.AreEqual(pdfDeliveryEvent.CreatedDateTime.Date, billRequestNotSentEvent.CreateDate.Date);
        Assert.AreEqual(pdfDeliveryEvent.DeliveryDateTime.Date, billRequestNotSentEvent.PdfDeliveryDate.Date);
        Assert.AreEqual(pdfDeliveryEvent.CreatedDateTime.Date, billRequestNotSentEvent.ReceivedDate.Date);
        
        // Validate Status Codes in Database
        var expectedIds = new List<int>
        {
            ExamStatusCodes.ExamPerformed.SpiroStatusCodeId,
            ExamStatusCodes.ClientPDFDelivered.SpiroStatusCodeId,
            ExamStatusCodes.BillRequestNotSent.SpiroStatusCodeId,
        };
        await ValidateExamStatusCodesByExamId(spiro.SpirometryExamId, expectedIds, 10, 3);
    }
    
    [RetryableTestMethod]
    [DynamicData(nameof(GetNotPerformedData))]
    public async Task ANC_T710_BillRequestNotSent_ExamNotPerformed(Dictionary<int, string> answersDict)
    {
        // Arrange
        var (member, appointment, evaluation) = await CoreApiActions.PrepareEvaluation();
        
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId, CoreApiActions.GetEvaluationAnswerList(answersDict));
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        
        // Publish the PDF event to the pdfdelivery topic
        var pdfDeliveryEvent = new PdfDeliveredToClient()
        {
            BatchId = 12345,
            BatchName = "Spiro_System_Tests" + DateTime.Now.Date.ToString("yyyyMMdd"),
            ProductCodes = new List<string>{"Spirometry"},
            CreatedDateTime = DateTime.Now,
            DeliveryDateTime = DateTime.Now,
            EvaluationId = evaluation.EvaluationId,
            EventId = DataGen.NewGuid()
        };
        CoreKafkaActions.PublishPdfDeliveryEvent(pdfDeliveryEvent,evaluation.EvaluationId.ToString());
        await Task.Delay(5000);
        
        // Validate record in ExamNotPerformed table
        var spiro = await GetNotPerformedRecordByEvaluationId(evaluation.EvaluationId);
        
        if (answersDict.TryGetValue(Answers.TechnicalIssueAnswerId, out var technicalIssueAnswer)) //implement TryGetValue per SonarQube suggestion
        {
            Assert.AreEqual(technicalIssueAnswer, spiro.NotPerformedReasonId.ToString());
        }
        else
        {
            Assert.AreEqual(answersDict[Answers.MemberRecentlyCompletedAnswerId], spiro.NotPerformedReasonId.ToString());
        }
        
        //  Validate the entry in the BillRequestSent table
        var exception = await Assert.ThrowsExceptionAsync<SpiroNotFoundException>( async () => await getBillingResultByEvaluationId(evaluation.EvaluationId) );
        Assert.AreEqual($"EvaluationId {evaluation.EvaluationId} not found in Spirometry BillRequestSent table", exception.Message);
        
        // Validate Kafka BillRequestNotSent event
        var billRequestNotSentEvent = await CoreKafkaActions.GetSpiroBillRequestNotSentEvent<BillRequestNotSent>(evaluation.EvaluationId);
        
        Assert.AreEqual("SPIROMETRY", billRequestNotSentEvent.ProductCode);
        Assert.AreEqual(evaluation.EvaluationId, billRequestNotSentEvent.EvaluationId);
        Assert.AreEqual(member.MemberPlanId.ToString(), billRequestNotSentEvent.MemberPlanId.ToString());
        Assert.AreEqual(Provider.ProviderId, billRequestNotSentEvent.ProviderId);
        Assert.AreEqual(pdfDeliveryEvent.CreatedDateTime.Date, billRequestNotSentEvent.CreateDate.Date);
        Assert.AreEqual(pdfDeliveryEvent.DeliveryDateTime.Date, billRequestNotSentEvent.PdfDeliveryDate.Date);
        Assert.AreEqual(pdfDeliveryEvent.CreatedDateTime.Date, billRequestNotSentEvent.ReceivedDate.Date);
        
        // Validate Status Codes in Database
        var expectedIds = new List<int>
        {
            ExamStatusCodes.ExamNotPerformed.SpiroStatusCodeId,
            ExamStatusCodes.ClientPDFDelivered.SpiroStatusCodeId,
            ExamStatusCodes.BillRequestNotSent.SpiroStatusCodeId,
        };
        await ValidateExamStatusCodesByExamId(spiro.SpirometryExamId, expectedIds, 10, 3);
    }
     private static IEnumerable<object[]> GetSpiroTestData
    {
        get
        {
            return new[]
            {
                new object[]
                {
                    new Dictionary<int, string>
                    {
                        { Answers.PerformedYesAnswerId, "Yes" },
                        { Answers.SessionGradeIdAnswerId, "B" },
                        { Answers.FVCAnswerId, "80" },
                        { Answers.FEV1AnswerId, "80" },
                        { Answers.FEV1FVCAnswerId, "0.65" },
                        { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                    },
                },
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.PerformedYesAnswerId, "Yes" },
                            { Answers.SessionGradeIdAnswerId, "B" },
                            { Answers.FVCAnswerId, "70" },
                            { Answers.FEV1AnswerId, "100" },
                            { Answers.FEV1FVCAnswerId, "0.7" },
                            { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                        },
                    ],
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.PerformedYesAnswerId, "Yes" },
                            { Answers.SessionGradeIdAnswerId, "A" },
                            { Answers.FVCAnswerId, "70" },
                            { Answers.FEV1AnswerId, "100" },
                            { Answers.FEV1FVCAnswerId, "0.7" },
                            { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                        },
                    ],
            };
        }
    }
     private static IEnumerable<object[]> GetResultsData
    {
        get
        {
            return new[]
            {
                new object[]
                {
                    new Dictionary<int, string>
                    {
                        { Answers.PerformedYesAnswerId, "Yes" },
                        { Answers.SessionGradeIdAnswerId, "D" },
                        { Answers.FVCAnswerId, "80" },
                        { Answers.FEV1AnswerId, "80" },
                        { Answers.FEV1FVCAnswerId, "0.65" },
                        { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                    },
                },
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.PerformedYesAnswerId, "Yes" },
                            { Answers.SessionGradeIdAnswerId, "E" },
                            { Answers.FVCAnswerId, "80" },
                            { Answers.FEV1AnswerId, "80" },
                            { Answers.FEV1FVCAnswerId, "0.65" },
                            { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                        },
                    ],
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.PerformedYesAnswerId, "Yes" },
                            { Answers.SessionGradeIdAnswerId, "F" },
                            { Answers.FVCAnswerId, "80" },
                            { Answers.FEV1AnswerId, "80" },
                            { Answers.FEV1FVCAnswerId, "0.65" },
                            { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                        },
                    ],
            };
        }
    }
     
     private static IEnumerable<object[]> GetNotPerformedData
    {
        get
        {
            return new[]
            {
                new object[]
                {
                    new Dictionary<int, string>
                    {
                        { Answers.PerformedNoAnswerId, "No" },
                        { Answers.UnablePerformAnswerId, "Unable to perform" },
                        { Answers.TechnicalIssueAnswerId, "5" },
                        { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                    },
                },
                    [
                        new Dictionary<int, string>
                        { 
                            { Answers.PerformedNoAnswerId, "No" },
                            { Answers.MemberRefusedAnswerId, "Member recently completed" },
                            { Answers.MemberRecentlyCompletedAnswerId, "1" },
                            { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                        },
                    ],
            };
        }
    }
  
}