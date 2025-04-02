using Signify.EvaluationsApi.Core.Values;
using Signify.eGFR.System.Tests.Core.Actions;
using Signify.eGFR.System.Tests.Core.Constants;
using Signify.QE.MSTest.Attributes;
using Signify.eGFR.Core.Events.Akka;
using Akka_OrderCreationEvent = Signify.eGFR.Core.Events.Akka.OrderCreationEvent;


namespace Signify.eGFR.System.Tests.Tests;

[TestClass, TestCategory("regression")]
public class OrderCreationTests : OrderCreationActions
{
     public TestContext TestContext { get; set; }

     [RetryableTestMethod]
     public async Task ANC_T816_Performed_KED_OrderCreation()
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
         
         // Validate OrderCreation Kafka event
         var ordercreated = await CoreKafkaActions.GetOrderCreationEvent<Akka_OrderCreationEvent>(evaluation.EvaluationId);
         ordercreated.ProductCode.Should().Be(TestConstants.Product);
         ordercreated.EvaluationId.Should().Be(evaluation.EvaluationId);
         ordercreated.EventId.Should().Be(ordercreated.EventId);
         ordercreated.Vendor.Should().Be(ordercreated.Vendor);
         ordercreated.Context.Should().BeSameAs(ordercreated.Context);
     }
     
     [RetryableTestMethod]
     public async Task ANC_T815_Performed_KED_Invalid_vendor_No_OrderCreation()
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
         // Database eGFR table
         var egfr = await GetExamByEvaluationId(evaluation.EvaluationId);
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
         
         // Validate orderCreatedEvent not present in the kafka
         var orderCreationEvent = await GetOrderCreationEvent(evaluation.EvaluationId);
         orderCreationEvent.Should().BeNull();
     }
}