using Signify.Spirometry.Svc.System.Tests.Core.Constants;
using Signify.Spirometry.Svc.System.Tests.Core.Models.Kafka;
using Signify.Spirometry.Svc.System.Tests.Core.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signify.QE.Core.Exceptions;

namespace Signify.Spirometry.Svc.System.Tests.Core.Actions;


public class ProviderPayActions: BaseTestActions
{
    protected async Task ValidateProviderPayable(int evaluationId, long memberPlanId)
    {
        // Database validations
        // ExamStatus
        var expectedStatusCodes = new List<int>
        {
            ExamStatusCodes.ExamPerformed.SpiroStatusCodeId,
            ExamStatusCodes.ProviderPayableEventReceived.SpiroStatusCodeId,
            ExamStatusCodes.ProviderPayRequestSent.SpiroStatusCodeId,
            ExamStatusCodes.CDIPassedReceived.SpiroStatusCodeId
        };
        var exam = await getSpiroExamByEvaluationId(evaluationId);
        
        await ValidateExamStatusCodesByExamId(exam.SpirometryExamId,
            expectedStatusCodes, 5, 2);
        
        //ProviderPay table
        var providerPay = await GetProviderPayByEvaluationId(evaluationId);
        Assert.IsNotNull(providerPay.PaymentId);
        
        // Kafka
        var providerPayStatusEvent =
            await CoreKafkaActions.GetSpiroProviderPayRequestSentEvent<ProviderPayRequestSent>(evaluationId);
        
        Assert.AreEqual(providerPay.PaymentId, providerPayStatusEvent.PaymentId);
        Assert.AreEqual(Product, providerPayStatusEvent.ProviderPayProductCode);
        Assert.AreEqual(Product, providerPayStatusEvent.ProductCode);
        Assert.AreEqual(evaluationId, providerPayStatusEvent.EvaluationId);
        Assert.AreEqual(memberPlanId, providerPayStatusEvent.MemberPlanId);
        Assert.AreEqual(Provider.ProviderId, providerPayStatusEvent.ProviderId);
        Assert.AreEqual(providerPay.CreatedDateTime.Date, providerPayStatusEvent.CreateDate.Date);
        Assert.AreEqual(providerPay.CreatedDateTime.Date, providerPayStatusEvent.ReceivedDate.Date);
        Assert.AreEqual(providerPay.CreatedDateTime.Date, providerPayStatusEvent.ParentEventDateTime.Date);
    } 
    public  async Task ValidateProviderNonPayable(int evaluationId)
    {
        // Database
        // ExamStatus
        List<int> expectedStatusCodes = [ExamStatusCodes.ProviderPayRequestSent.SpiroStatusCodeId];
        var exam = await getSpiroExamByEvaluationId(evaluationId);
        
         await Assert.ThrowsExceptionAsync<ExamStatusCodeNotFoundException>(async () =>
         {
             await ValidateExamStatusCodesByExamId(exam.SpirometryExamId, expectedStatusCodes, 5, 2);
         });
         
        //ProviderPay table
        await Assert.ThrowsExceptionAsync<ProviderPayNotFoundException>(async () =>
        {
            await GetProviderPayByEvaluationId(evaluationId);
        });
        
        // Kafka
        // Remove once  kafka validation in prod is enabled
        if (Environment.GetEnvironmentVariable("TEST_ENV").Equals("prod")) return;
        await Assert.ThrowsExceptionAsync<KafkaEventsNotFoundException>(async () =>
        {
            await CoreKafkaActions.GetSpiroProviderPayRequestSentEvent<ProviderPayRequestSent>(evaluationId);
        });
    }
}