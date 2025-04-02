using Signify.PAD.Svc.System.Tests.Core.Constants;
using Signify.PAD.Svc.System.Tests.Core.Exceptions;
using Signify.PAD.Svc.System.Tests.Core.Models.Kafka;
using Signify.QE.Core.Exceptions;

namespace Signify.PAD.Svc.System.Tests.Core.Actions;

public class ProviderPayActions : BaseTestActions
{
    protected async Task ValidateProviderPayable(int evaluationId, long memberPlanId)
    {
        // Database validations
        // ExamStatus
        var expectedStatusCodes = new List<int>
        {
            ExamStatusCodes.ProviderPayableEventReceived.PADStatusCodeId,
            ExamStatusCodes.ProviderPayRequestSent.PADStatusCodeId,
            ExamStatusCodes.CdiPassedReceived.PADStatusCodeId
        };
        var exam = await GetPadByEvaluationId(evaluationId, 10, 2);
        
        await ValidateExamStatusCodesByExamId(exam.PADId,
            expectedStatusCodes, 5, 2);
        
        //ProviderPay table
        var providerPay = await GetProviderPayResultsByExamId(exam.PADId, 5, 2);
        Assert.IsNotNull(providerPay.PaymentId);
        
        // Kafka
        var providerPayStatusEvent = await CoreKafkaActions.GetProviderPayRequestSentEvent<ProviderPayRequestSent>(evaluationId);
        Assert.AreEqual(providerPay.PaymentId, providerPayStatusEvent.PaymentId);
        Assert.AreEqual(Product, providerPayStatusEvent.ProviderPayProductCode);
        // providerPayStatusEvent.ParentEventDateTime.UtcDateTime.Should().BeCloseTo(cdiEvent.DateTime.UtcDateTime,TimeSpan.FromSeconds(0.5));
        Assert.AreEqual(Product, providerPayStatusEvent.ProductCode);
        Assert.AreEqual(evaluationId, providerPayStatusEvent.EvaluationId);
        Assert.AreEqual(memberPlanId, providerPayStatusEvent.MemberPlanId);
        Assert.AreEqual(Provider.ProviderId, providerPayStatusEvent.ProviderId);

        var diff = Math.Abs((providerPayStatusEvent.CreateDate.ToUniversalTime() - providerPay.CreatedDateTime).TotalMinutes);
        Assert.IsTrue(diff <= 1);
        diff = Math.Abs((providerPayStatusEvent.ReceivedDate.ToUniversalTime() - providerPay.CreatedDateTime).TotalMinutes);
        Assert.IsTrue(diff <= 1);
    }
    
    protected async Task ValidateProviderNonPayable(int evaluationId)
    {
        // Database
        // ExamStatus
        List<int> expectedStatusCodes = [ExamStatusCodes.ProviderPayRequestSent.PADStatusCodeId];
        var exam = await GetPadByEvaluationId(evaluationId, 20, 2);

        await Assert.ThrowsExceptionAsync<ExamStatusCodeNotFoundException>(async () =>
            await ValidateExamStatusCodesByExamId(exam.PADId, expectedStatusCodes, 5, 2));
        
        //ProviderPay table
        await Assert.ThrowsExceptionAsync<ProviderPayNotFoundException>(async () =>
            await GetProviderPayResultsByExamId(exam.PADId, 5, 2));
        
        // Kafka
        await Assert.ThrowsExceptionAsync<KafkaEventsNotFoundException>(async () =>
            await CoreKafkaActions.GetProviderPayRequestSentEvent<ProviderPayActions>(evaluationId));
    }
}