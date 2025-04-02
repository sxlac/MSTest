using FluentAssertions;
using Signify.HBA1CPOC.Messages.Events.Akka;
using Signify.HBA1CPOC.System.Tests.Core.Constants;
using Signify.MemberApi.Core.DTO;
using Appointment = Signify.QE.Core.Models.Appointment.Appointment;

namespace Signify.HBA1CPOC.System.Tests.Core.Actions;

public class BillingActions: BaseTestActions
{
    protected async Task Validate_Kafka_event_expected_billable_status(int evaluation)
    {
        var receivedEvent = await CoreKafkaActions.GetA1CpocResultsReceivedEvent<ResultsReceived>(evaluation);
        receivedEvent.ProductCode.Should().Be(TestConstants.Product);
        receivedEvent.IsBillable.Should().Be(true);
    }
    
    protected void Validate_entry_using_EvaluationId_in_HBA1CPOC(int evaluation, string normality,
        MemberDto member, Appointment appointment, Dictionary<int,string> answers)
    {
        var billingRecord = GetHba1CpocRecordByEvaluationId(evaluation);
        billingRecord.EvaluationId.Should().Be(evaluation);
        billingRecord.A1CPercent.Should().Be(answers[Answers.Qid91491PercentA1CAnswerId]);
        billingRecord.NormalityIndicator.Should().Be(normality);
        billingRecord.MemberPlanId.Should().Be(member.MemberPlanId);
        billingRecord.CenseoId.Should().Be(member.CenseoId);
        billingRecord.AppointmentId.Should().Be(appointment.AppointmentId);
        billingRecord.ProviderId.Should().Be(TestConstants.Provider.ProviderId);
        billingRecord.ReceivedDateTime.Should().BeSameDateAs(Convert.ToDateTime(answers[Answers.DoSAnswerId]));
        billingRecord.ExpirationDate.Should().BeSameDateAs(Convert.ToDateTime(answers[Answers.Qid91551ExpirationDateAnswerId]));
        member.DateOfBirth.Should().BeSameDateAs(billingRecord.DateOfBirth);
    }
}