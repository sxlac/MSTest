using Signify.QE.MSTest.Attributes;
using Signify.EvaluationsApi.Core.Values;
using Signify.HBA1CPOC.Messages.Events;
using Signify.HBA1CPOC.Messages.Events.Status;
using Signify.HBA1CPOC.Svc.Core.Data.Entities;
using Signify.HBA1CPOC.System.Tests.Core.Constants;
using Signify.HBA1CPOC.System.Tests.Core.Models.Kafka;

namespace Signify.HBA1CPOC.System.Tests.Tests;

[TestClass, TestCategory("regression")]
public class Billing : BillingActions
{
    public TestContext TestContext { get; set; }

    [RetryableTestMethod]
    [DataRow("4", "N")]
    public async Task ANC_T326_Hba1cpocBillable(string answerValue, string normality)
    {
        
        TestContext.WriteLine("HBA1CPOC Billable");
        // Arrange
        var (member,appointment,evaluation) = await CoreApiActions.PrepareEvaluation();
        var datestamp = DateTime.Now;
        var answers = GeneratePerformedAnswers(percentA1C:answerValue);
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId, CoreApiActions.GetEvaluationAnswerList(answers));

        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        
        // Assert
        var eval = CoreApiActions.GetEvaluation(evaluation.EvaluationId);
        eval.Status.Should().Be(Completed);

        // Validate the entry using EvaluationId in HBA1CPOC & HBA1CPOCStatus tables.
        Validate_entry_using_EvaluationId_in_HBA1CPOC(evaluation.EvaluationId, normality, member, appointment, answers);
        
        
        // Validate that the Kafka event has the expected billable status 
        Validate_Kafka_event_expected_billable_status(evaluation.EvaluationId);
        
        
        var pdfEventValue = new PdfDeliveredToClient()
        {
            BatchId = 12345,
            BatchName = "Hba1CPOC_System_Tests" + DateTime.Now.Date.ToString("yyyyMMdd"),
            ProductCodes = ["HBA1CPOC"],
            CreatedDateTime = DateTime.Now,
            DeliveryDateTime = DateTime.Now,
            EvaluationId = evaluation.EvaluationId,
            EventId = DataGen.NewUuid()
        };
        // Publish the PDF event to the pdfdelivery topic
        CoreKafkaActions.PublishPdfDeliveryEvent(pdfEventValue, evaluation.EvaluationId.ToString());
       
        
        // Validate that the billing details are as expected using EvaluationId in HBA1CPOCBilling table
        var billRecord = GetBillingResultsByEvaluationId(evaluation.EvaluationId);
        billRecord.ProviderId.Should().Be(TestConstants.Provider.ProviderId);
        billRecord.AddressLineOne.Should().Be(member.AddressLineOne);
        billRecord.EvaluationId.Should().Be(evaluation.EvaluationId);
        billRecord.MemberId.Should().Be(member.MemberId);
        billRecord.CenseoId.Should().Be(member.CenseoId);
        billRecord.ClientId.Should().Be(14);
        billRecord.AddressLineTwo.Should().Be(member.AddressLineTwo);
        billRecord.MemberPlanId.Should().Be(member.MemberPlanId);
        billRecord.UserName.Should().Be(UserName);
        billRecord.FirstName.Should().Be(member.FirstName);
        billRecord.ZipCode.Should().Be(member.ZipCode);
        billRecord.NationalProviderIdentifier.Should().Be(TestConstants.Provider.NationalProviderIdentifier);
        billRecord.City.Should().Be(member.City);
        billRecord.MiddleName.Should().Be(member.MiddleName);
        billRecord.AppointmentId.Should().Be(appointment.AppointmentId); 
        billRecord.State.Should().Be(member.State);  
        billRecord.BillId.Should().NotBe(null);   
        billRecord.LastName.Should().Be(member.LastName);    
        billRecord.ApplicationId.Should().Be(Application);    
       
        
        var expectedStatusCodes = new List<int>
        {
            HBA1CPOCStatusCode.HBA1CPOCPerformed.HBA1CPOCStatusCodeId,
            HBA1CPOCStatusCode.BillRequestSent.HBA1CPOCStatusCodeId,
            HBA1CPOCStatusCode.BillableEventRecieved.HBA1CPOCStatusCodeId, 
        };
        ValidateExamStatusCodesByExamId(billRecord.HBA1CPOCId,expectedStatusCodes);
        
        // Validate BillRequestSent Message in Kafka
        var performedEvent = await CoreKafkaActions.GetA1CpocBillRequestSentEvent<BillRequestSent>(evaluation.EvaluationId);
        performedEvent.EvaluationId.Should().Be(evaluation.EvaluationId);
        performedEvent.MemberPlanId.Should().Be(appointment.MemberPlanId);
        performedEvent.ProviderId.ToString().Should().Be(appointment.ProviderId); 
        performedEvent.PdfDeliveryDate.Should().BeSameDateAs(datestamp);  
        performedEvent.CreateDate.Should().BeSameDateAs(datestamp);  
        performedEvent.ReceivedDate.Should().BeSameDateAs(datestamp);   
    }
   
    [Ignore]
    [RetryableTestMethod]
    [DataRow("4", "N")]
    public async Task ANC_T692_Hba1cpoc_Bill_Accepted_with_billId_present(string answerValue, string normality)
    {
        
        TestContext.WriteLine("HBA1CPOC Bill Accepted with billId present");
        // Arrange
        var (member,appointment,evaluation) = await CoreApiActions.PrepareEvaluation();
        var answers = GeneratePerformedAnswers(percentA1C:answerValue);
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId, CoreApiActions.GetEvaluationAnswerList(answers));

        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        
        // Assert
        var eval = CoreApiActions.GetEvaluation(evaluation.EvaluationId);
        eval.Status.Should().Be(Completed);

        // Validate the entry using EvaluationId in HBA1CPOC & HBA1CPOCStatus tables.
        Validate_entry_using_EvaluationId_in_HBA1CPOC(evaluation.EvaluationId, normality, member, appointment, answers);

        
        // Validate that the Kafka event has the expected billable status 

        await Validate_Kafka_event_expected_billable_status(evaluation.EvaluationId);
        
        var pdfEventValue = new PdfDeliveredToClient()
        {
            BatchId = 12345,
            BatchName = "Hba1CPOC_System_Tests" + DateTime.Now.Date.ToString("yyyyMMdd"),
            ProductCodes = ["HBA1CPOC"],
            CreatedDateTime = DateTime.Now,
            DeliveryDateTime = DateTime.Now,
            EvaluationId = evaluation.EvaluationId,
            EventId = DataGen.NewUuid()
        };
        // Publish the PDF event to the pdfdelivery topic
        CoreKafkaActions.PublishPdfDeliveryEvent(pdfEventValue, evaluation.EvaluationId.ToString());
       
        
        // Validate that the billing details are as expected using EvaluationId in HBA1CPOCBilling table
        var billRecord = GetBillingResultsByEvaluationId(evaluation.EvaluationId);
        billRecord.ProviderId.Should().Be(TestConstants.Provider.ProviderId);
        billRecord.AddressLineOne.Should().Be(member.AddressLineOne);
        billRecord.EvaluationId.Should().Be(evaluation.EvaluationId);
        billRecord.MemberId.Should().Be(member.MemberId);
        billRecord.CenseoId.Should().Be(member.CenseoId);
        billRecord.ClientId.Should().Be(14);
        billRecord.AddressLineTwo.Should().Be(member.AddressLineTwo);
        billRecord.MemberPlanId.Should().Be(member.MemberPlanId);
        billRecord.UserName.Should().Be(UserName);
        billRecord.FirstName.Should().Be(member.FirstName);
        billRecord.ZipCode.Should().Be(member.ZipCode);
        billRecord.NationalProviderIdentifier.Should().Be(TestConstants.Provider.NationalProviderIdentifier);
        billRecord.City.Should().Be(member.City);
        billRecord.MiddleName.Should().Be(member.MiddleName);
        billRecord.AppointmentId.Should().Be(appointment.AppointmentId); 
        billRecord.State.Should().Be(member.State);  
        billRecord.BillId.Should().NotBe(null);   
        billRecord.LastName.Should().Be(member.LastName);    
        billRecord.ApplicationId.Should().Be(EvaluationFinalizedEvent);


        var rcmBillId = billRecord.BillId;
        var billAcceptedValue = new BillAcceptedValue()
        {
            RCMProductCode = ["HBA1CPOC"],
            RCMBillId = rcmBillId, 
        };
        
        // Publish the PDF event to the pdfdelivery topic
        CoreKafkaActions.PublishEvent("rcm_bill", billAcceptedValue,"bill-" + rcmBillId,"BillRequestAccepted");
        
        var acceptedBillRecord = GetBillingResultsByEvaluationId(evaluation.EvaluationId);
        acceptedBillRecord.AcceptedAt.Should().NotBe(null);  
        acceptedBillRecord.Accepted.Should().Be(true);  
    }
    
    [Ignore]
    [RetryableTestMethod]
    [DataRow("4", "N")]
    public async Task ANC_T692_Hba1cpoc_Bill_Accepted_billId_not_present(string answerValue, string normality)
    {
        
        TestContext.WriteLine("HBA1CPOC Bill Accepted billId not present");
        // Arrange
        var (member,appointment,evaluation) = await CoreApiActions.PrepareEvaluation();
        
        var answers = GeneratePerformedAnswers(percentA1C:answerValue);
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId, CoreApiActions.GetEvaluationAnswerList(answers));

        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        
        // Assert
        var eval = CoreApiActions.GetEvaluation(evaluation.EvaluationId);
        eval.Status.Should().Be(Completed);

        // Validate the entry using EvaluationId in HBA1CPOC & HBA1CPOCStatus tables.
        Validate_entry_using_EvaluationId_in_HBA1CPOC(evaluation.EvaluationId, normality, member, appointment, answers);
        
        // Validate that the Kafka event has the expected billable status 
        await Validate_Kafka_event_expected_billable_status(evaluation.EvaluationId);
        
        var pdfEventValue = new PdfDeliveredToClient()
        {
            BatchId = 12345,
            BatchName = "Hba1CPOC_System_Tests" + DateTime.Now.Date.ToString("yyyyMMdd"),
            ProductCodes = ["HBA1CPOC"],
            CreatedDateTime = DateTime.Now,
            DeliveryDateTime = DateTime.Now,
            EvaluationId = evaluation.EvaluationId,
            EventId = DataGen.NewUuid()
        };
        // Publish the PDF event to the pdfdelivery topic
        CoreKafkaActions.PublishPdfDeliveryEvent(pdfEventValue, evaluation.EvaluationId.ToString());
       
        
        // Validate that the billing details are as expected using EvaluationId in HBA1CPOCBilling table
        var billRecord = GetBillingResultsByEvaluationId(evaluation.EvaluationId);
        billRecord.ProviderId.Should().Be(TestConstants.Provider.ProviderId);
        billRecord.AddressLineOne.Should().Be(member.AddressLineOne);
        billRecord.EvaluationId.Should().Be(evaluation.EvaluationId);
        billRecord.MemberId.Should().Be(member.MemberId);
        billRecord.CenseoId.Should().Be(member.CenseoId);
        billRecord.ClientId.Should().Be(14);
        billRecord.AddressLineTwo.Should().Be(member.AddressLineTwo);
        billRecord.MemberPlanId.Should().Be(member.MemberPlanId);
        billRecord.UserName.Should().Be(UserName);
        billRecord.FirstName.Should().Be(member.FirstName);
        billRecord.ZipCode.Should().Be(member.ZipCode);
        billRecord.NationalProviderIdentifier.Should().Be(TestConstants.Provider.NationalProviderIdentifier);
        billRecord.City.Should().Be(member.City);
        billRecord.MiddleName.Should().Be(member.MiddleName);
        billRecord.AppointmentId.Should().Be(appointment.AppointmentId); 
        billRecord.State.Should().Be(member.State);  
        billRecord.BillId.Should().NotBe(null);   
        billRecord.LastName.Should().Be(member.LastName);    
        billRecord.ApplicationId.Should().Be(EvaluationFinalizedEvent);


        var rcmBillId = DataGen.NewUuid();
        var billAcceptedValue = new BillAcceptedValue()
        {
            RCMProductCode = ["HBA1CPOC"],
            RCMBillId = rcmBillId.ToString(), 
        };
        
        // Publish the PDF event to the pdfdelivery topic
       CoreKafkaActions.PublishEvent("rcm_bill", billAcceptedValue,"bill-" + rcmBillId,"BillRequestAccepted");
        
       var acceptedBillRecord = GetBillingResultsByEvaluationId(evaluation.EvaluationId);
       acceptedBillRecord.AcceptedAt.Should().Be(null);  
       acceptedBillRecord.Accepted.ToString().Should().Be(null);  
    } 
    
}