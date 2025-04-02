using FluentAssertions;
using Signify.FOBT.Svc.System.Tests.Core.Constants;
using Signify.FOBT.Svc.System.Tests.Core.Exceptions;
using Signify.FOBT.Svc.System.Tests.Core.Models.Kafka;

namespace Signify.FOBT.Svc.System.Tests.Core.Actions;

public class ProviderPayActions: BaseTestActions
{
    protected async Task ValidateProviderPayable(int evaluationId, long memberPlanId)
    {
        // Database validations
        // ExamStatus
        var expectedStatusCodes = new List<int>
        {
            ExamStatusCodes.ProviderPayableEventReceived.FOBTStatusCodeId,
            ExamStatusCodes.ProviderPayRequestSent.FOBTStatusCodeId,
            ExamStatusCodes.CdiPassedReceived.FOBTStatusCodeId
        };
        var exam = await GetFOBTByEvaluationId(evaluationId, 10, 2);
        
        await ValidateExamStatusCodesByExamId(exam.FOBTId,
            expectedStatusCodes, 5, 2);
        
        //ProviderPay table
        var providerPay = await GetProviderPayResultsByExamId(exam.FOBTId, 5, 2);
        providerPay.PaymentId.Should().NotBe(null);
        
        // Kafka
        var providerPayStatusEvent =
            await CoreKafkaActions.GetProviderPayRequestSentEvent<ProviderPayRequestSent>(evaluationId);
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
    public  async Task ValidateProviderNonPayable(int evaluationId)
    {
        // Database
        // ExamStatus
        List<int> expectedStatusCodes = [ExamStatusCodes.ProviderPayRequestSent.FOBTStatusCodeId];
        var exam = await GetFOBTByEvaluationId(evaluationId, 20, 2);
        
        this.Invoking(t=>t.ValidateExamStatusCodesByExamId(exam.FOBTId, expectedStatusCodes, 5, 2))
            .Should().ThrowAsync<ExamStatusCodeNotFoundException>();
        
        //ProviderPay table
        this.Invoking(t=>t.GetProviderPayResultsByExamId(exam.FOBTId, 5, 2))
            .Should().ThrowAsync<ProviderPayNotFoundException>();
        
        // Kafka
        // Remove once  kafka validation in prod is enabled
        if (Environment.GetEnvironmentVariable("TEST_ENV").Equals("prod")) return;
        this.Invoking(async t => CoreKafkaActions.GetProviderPayRequestSentEvent<ProviderPayActions>(evaluationId))
            .Should().ThrowAsync<NullReferenceException>();
    }
}
    
    
