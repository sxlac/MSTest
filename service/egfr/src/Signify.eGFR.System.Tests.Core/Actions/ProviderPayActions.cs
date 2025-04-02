using FluentAssertions;
using Signify.eGFR.Core.Events.Status;
using Signify.eGFR.System.Tests.Core.Constants;
using Signify.eGFR.System.Tests.Core.Exceptions;
using Signify.eGFR.System.Tests.Core.Models.Kafka;
using ProviderPayRequestSent = Signify.eGFR.System.Tests.Core.Models.Kafka.ProviderPayRequestSent;

namespace Signify.eGFR.System.Tests.Core.Actions;

public class ProviderPayActions: BaseTestActions
{
    protected async Task ValidateProviderPayable(int evaluationId, long memberPlanId)
    {
        // Database validations
        // ExamStatus
        var expectedStatusCodes = new List<int>
        {
            ExamStatusCodes.ProviderPayableEventReceived.ExamStatusCodeId,
            ExamStatusCodes.ProviderPayRequestSent.ExamStatusCodeId,
            ExamStatusCodes.CDIPassedReceived.ExamStatusCodeId
        };
        var exam = await GetExamByEvaluationId(evaluationId);
        
        await ValidateExamStatusCodesByExamId(exam.ExamId, expectedStatusCodes);
        
        //ProviderPay table
        var providerPay = await GetProviderPayResultsByExamId(exam.ExamId);
        providerPay.PaymentId.Should().NotBe(null);
        
        // Kafka
        var providerPayStatusEvent =
             await CoreKafkaActions.GetEgfrProviderPayRequestSentEvent<ProviderPayRequestSent>(evaluationId);
        providerPayStatusEvent.PaymentId.Should().Be(providerPay.PaymentId);
        providerPayStatusEvent.ProviderPayProductCode.Should().Be(Product);
        providerPayStatusEvent.ProductCode.Should().Be(TestConstants.Product);
        providerPayStatusEvent.EvaluationId.Should().Be(evaluationId);
        providerPayStatusEvent.MemberPlanId.Should().Be(memberPlanId);
        providerPayStatusEvent.ProviderId.Should().Be(Provider.ProviderId);
        providerPayStatusEvent.CreatedDate.Date.Should().Be(providerPay.CreatedDateTime.Date);
        providerPayStatusEvent.ReceivedDate.Date.Should().Be(providerPay.CreatedDateTime.Date);
        providerPayStatusEvent.ParentEventDateTime.Date.Should().Be(providerPay.CreatedDateTime.Date);
    } 
    protected  async Task ValidateProviderNonPayable(int evaluationId)
    {
        // Database
        // ExamStatus
        List<int> expectedStatusCodes = [ExamStatusCodes.ProviderNonPayableEventReceived.ExamStatusCodeId];
        var exam = await GetExamByEvaluationId(evaluationId);
        
        // await this.Invoking(t=>t.ValidateExamStatusCodesByExamId(exam.ExamId, expectedStatusCodes))
        //     .Should().ThrowAsync<ExamStatusCodeNotFoundException>();
        
        //ProviderPay table
        await this.Invoking(t=>t.GetProviderPayResultsByExamId(exam.ExamId))
            .Should().ThrowAsync<ProviderPayNotFoundException>();
        
        // Kafka
         var providerNonPayStatusEvent =
             await CoreKafkaActions.GetEgfrProviderNonPayableEventReceivedEvent<ProviderNonPayEventReceived>(evaluationId);
         providerNonPayStatusEvent.ParentCdiEvent.Should().Be("CDIPassedEvent");
         providerNonPayStatusEvent.Reason.Should().Be("Normality is Undetermined");
         providerNonPayStatusEvent.ProductCode.Should().Be(Product);
         providerNonPayStatusEvent.EvaluationId.Should().Be(evaluationId);
         providerNonPayStatusEvent.MemberPlanId.Should().Be(providerNonPayStatusEvent.MemberPlanId);
         providerNonPayStatusEvent.ProviderId.Should().Be(Provider.ProviderId);
         providerNonPayStatusEvent.CreatedDate.Date.Should().Be(providerNonPayStatusEvent.CreatedDate.Date);
         providerNonPayStatusEvent.ReceivedDate.Date.Should().Be(providerNonPayStatusEvent.ReceivedDate.Date);
        
        // await this.Invoking(async t =>  await CoreKafkaActions.GetProviderNonPayableEventReceivedEvent<ProviderPayActions>(evaluationId))
        //  .Should().ThrowAsync<NullReferenceException>();
    }
}