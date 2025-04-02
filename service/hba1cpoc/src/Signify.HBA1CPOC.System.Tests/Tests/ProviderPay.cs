using Signify.Dps.Test.Utilities.Database.Exceptions;
using Signify.QE.MSTest.Attributes;
using Signify.EvaluationsApi.Core.Values;
using Signify.HBA1CPOC.Messages.Events.Akka;
using Signify.HBA1CPOC.Messages.Events.Status;
using Signify.HBA1CPOC.Svc.Core.Data.Entities;
using Signify.HBA1CPOC.System.Tests.Core.Constants;
using Signify.QE.Core.Exceptions;

namespace Signify.HBA1CPOC.System.Tests.Tests;

[TestClass, TestCategory("regression")]
public class ProviderPay: ProviderPayActions
{
    public TestContext TestContext { get; set; }

    [RetryableTestMethod]
    [DataRow("3.9", "A")]
    [DataRow("4", "N")]
    [DataRow("6.9", "N")]
    [DataRow("7", "A")]
    public async Task ANC_T324_Hba1cpocProviderPay(string answerValue, string normality)
    {
        
        TestContext.WriteLine("HBA1CPOC Provider Pay (Business rules met) - (CDIPassedEvent)" +
                              "With PayProvider: True)");
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

        // Validate the entry using EvaluationId in HBA1CPOC
        var record = GetHba1CpocRecordByEvaluationId(evaluation.EvaluationId);
        record.EvaluationId.Should().Be(evaluation.EvaluationId);
        
        var providerPayRecord = GetProviderPayResultsWithEvalId(evaluation.EvaluationId);
        providerPayRecord.ProviderId.Should().Be(Provider.ProviderId);
        providerPayRecord.AddressLineOne.Should().Be(member.AddressLineOne);
        providerPayRecord.EvaluationId.Should().Be(evaluation.EvaluationId);
        providerPayRecord.MemberId.Should().Be(member.MemberId);
        providerPayRecord.CenseoId.Should().Be(member.CenseoId);
        providerPayRecord.ClientId.Should().Be(14);
        providerPayRecord.AddressLineTwo.Should().Be(member.AddressLineTwo);
        providerPayRecord.MemberPlanId.Should().Be(member.MemberPlanId);
        providerPayRecord.UserName.Should().Be(UserName);
        providerPayRecord.FirstName.Should().Be(member.FirstName);
        providerPayRecord.ZipCode.Should().Be(member.ZipCode);
        providerPayRecord.NationalProviderIdentifier.Should().Be(Provider.NationalProviderIdentifier);
        providerPayRecord.City.Should().Be(member.City);
        providerPayRecord.MiddleName.Should().Be(member.MiddleName);
        providerPayRecord.AppointmentId.Should().Be(appointment.AppointmentId); 
        providerPayRecord.State.Should().Be(member.State);  
        providerPayRecord.PaymentId.Should().NotBe(null);   
        providerPayRecord.LastName.Should().Be(member.LastName);    
        providerPayRecord.ApplicationId.Should().Be(Application);
        
        var expectedStatusCodes = new List<int>
        {
            HBA1CPOCStatusCode.HBA1CPOCPerformed.HBA1CPOCStatusCodeId,
            HBA1CPOCStatusCode.CdiPassedReceived.HBA1CPOCStatusCodeId,
            HBA1CPOCStatusCode.ProviderPayableEventReceived.HBA1CPOCStatusCodeId,
            HBA1CPOCStatusCode.ProviderPayRequestSent.HBA1CPOCStatusCodeId
        };
        
        ValidateExamStatusCodesByExamId(providerPayRecord.HBA1CPOCId, expectedStatusCodes);
        //  Validate that a Kafka event - ProviderPayRequestSent
        (await GetProviderPayRequestSentEvent(evaluation.EvaluationId)).EvaluationId.Should().Be(evaluation.EvaluationId);
        
        //  Validate that the Kafka event - ProviderPayableEventReceived
        (await GetProviderPayableEventReceivedEvent(evaluation.EvaluationId)).EvaluationId.Should().Be(evaluation.EvaluationId); 
    }
    
    [RetryableTestMethod]
    [DataRow("4", "N", "passedPastExp", "ExpirationDate is before DateOfService")] 
    public async Task ANC_T581_Hba1cpocProviderPay_passedPastExp(string answerValue, string normality, string testScenario, string reason)
    {
        TestContext.WriteLine($"HBA1CPOC Non-Payable (Business rules met/not met) - (CDIPassedEvent) " +
                              $"and (CDIFailedEvent) '{testScenario}' <payProvider>");
        // Arrange
        var (member,appointment,evaluation) = await CoreApiActions.PrepareEvaluation();
        
        var answers = GeneratePerformedAnswers(answerValue, DateTime.Now.AddDays(-5).ToString("O"));
        
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId, CoreApiActions.GetEvaluationAnswerList(answers));

        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        // Assert
        var eval = CoreApiActions.GetEvaluation(evaluation.EvaluationId);
        eval.Status.Should().Be(Completed);

        // Validate the entry using EvaluationId in HBA1CPOC & HBA1CPOCStatus tables. Status 1 = HBA1CPOCPerformed 
        var record = GetHba1CpocRecordByEvaluationId(evaluation.EvaluationId);
        record.EvaluationId.Should().Be(evaluation.EvaluationId);
        
        var providerPayRecord = GetProviderPayResultsWithEvalId(evaluation.EvaluationId);
        providerPayRecord.Should().BeNull();
        
        //  Validate that a Kafka event - ProviderPayRequestSent - was not raised
        (await GetProviderPayRequestSentEvent(evaluation.EvaluationId)).Should().BeNull();
        //  Validate that the Kafka event - ProviderPayableEventReceived - was not raised
        (await GetProviderPayableEventReceivedEvent(evaluation.EvaluationId)).Should().BeNull();
       
        // Validate that the Kafka event - ProviderNonPayableEventReceived - was raised
        var providerNonPayableEventReceivedEvent = await CoreKafkaActions.GetA1CpocProviderNonPayableEventReceivedEvent<ProviderNonPayableEventReceived>(evaluation.EvaluationId);
        providerNonPayableEventReceivedEvent.EvaluationId.Should().Be(evaluation.EvaluationId);
        providerNonPayableEventReceivedEvent.ProviderId.Should().Be(Provider.ProviderId);
        providerNonPayableEventReceivedEvent.ParentCdiEvent.Should().Be("CDIPassedEvent");
        providerNonPayableEventReceivedEvent.MemberPlanId.Should().Be(member.MemberPlanId);
        providerNonPayableEventReceivedEvent.Reason.Should().Be(reason);
        providerNonPayableEventReceivedEvent.ProductCode.Should().Be(Product);
        providerNonPayableEventReceivedEvent.CreateDate.Should().BeSameDateAs(record.CreatedDateTime);
        providerNonPayableEventReceivedEvent.ReceivedDate.Should().BeSameDateAs(record.ReceivedDateTime);
        
        var expectedStatusCodes = new List<int>
        {
            HBA1CPOCStatusCode.HBA1CPOCPerformed.HBA1CPOCStatusCodeId,
            HBA1CPOCStatusCode.CdiPassedReceived.HBA1CPOCStatusCodeId,
            HBA1CPOCStatusCode.ProviderNonPayableEventReceived.HBA1CPOCStatusCodeId
        };
        ValidateExamStatusCodesByExamId(record.HBA1CPOCId, expectedStatusCodes);
        
        var unExpectedStatusCodes = new List<int>
        {
            HBA1CPOCStatusCode.ProviderPayableEventReceived.HBA1CPOCStatusCodeId,
            HBA1CPOCStatusCode.ProviderPayRequestSent.HBA1CPOCStatusCodeId,
        }; 
        ValidateExamStatusCodesNotPresentByExamId(record.HBA1CPOCId, unExpectedStatusCodes);
    }
    
    [RetryableTestMethod]
    [DataRow("4", "N", "passedNullDos", "Invalid ExpirationDate or DateOfService")] 
    public async Task ANC_T578_Hba1cpocProviderPay_passedNullDos(string answerValue, string normality, string testScenario, string reason)
    {
        TestContext.WriteLine($"HBA1CPOC Non-Payable (Business rules met/not met) - (CDIPassedEvent) " +
                              $"'{testScenario}' <payProvider>");
        // Arrange
        var (member,appointment,evaluation) = await CoreApiActions.PrepareEvaluation();
        
        var answers = GeneratePerformedAnswers(percentA1C:answerValue);
        answers[Answers.DoSAnswerId] = "";
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId, CoreApiActions.GetEvaluationAnswerList(answers));

        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        // Assert
        
        var eval = CoreApiActions.GetEvaluation(evaluation.EvaluationId);
        eval.Status.Should().Be(Completed);

        // Validate the entry using EvaluationId in HBA1CPOC & HBA1CPOCStatus tables. Status 1 = HBA1CPOCPerformed 
        var record = GetHba1CpocRecordByEvaluationId(evaluation.EvaluationId);
        record.EvaluationId.Should().Be(evaluation.EvaluationId);
        
        var providerPayRecord = GetProviderPayResultsWithEvalId(evaluation.EvaluationId);
        providerPayRecord.Should().BeNull();
        
        //  Validate that a Kafka event - ProviderPayRequestSent - was not raised
        this.Invoking(async t=> await CoreKafkaActions.GetA1CpocProviderPayRequestSentEvent<ProviderPayRequestSent>(evaluation.EvaluationId))
            .Should().ThrowAsync<KafkaEventsNotFoundException>().GetAwaiter().GetResult()
            .WithMessage($"Unable to find any consumed events from the A1CPOC_Status topic matching key: {evaluation.EvaluationId} value:  type: ProviderPayRequestSent");
        (await GetProviderPayRequestSentEvent(evaluation.EvaluationId)).Should().BeNull();
        //  Validate that the Kafka event - ProviderPayableEventReceived - was not raised
        (await GetProviderPayableEventReceivedEvent(evaluation.EvaluationId)).Should().BeNull();
        
        var expectedStatusCodes = new List<int>
        {
            HBA1CPOCStatusCode.HBA1CPOCPerformed.HBA1CPOCStatusCodeId,
        };
        ValidateExamStatusCodesByExamId(record.HBA1CPOCId, expectedStatusCodes);
        
        var unExpectedStatusCodes = new List<int>
        {
            HBA1CPOCStatusCode.ProviderPayableEventReceived.HBA1CPOCStatusCodeId,
            HBA1CPOCStatusCode.ProviderPayRequestSent.HBA1CPOCStatusCodeId,
        }; 
        ValidateExamStatusCodesNotPresentByExamId(record.HBA1CPOCId, unExpectedStatusCodes);
    }
        
}