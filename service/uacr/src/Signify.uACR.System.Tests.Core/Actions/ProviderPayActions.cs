using FluentAssertions;
using Signify.Dps.Test.Utilities.Database.Exceptions;
using Signify.uACR.Core.Events.Status;
using Signify.uACR.System.Tests.Core.Constants;

namespace Signify.uACR.System.Tests.Core.Actions;

public class ProviderPayActions : BaseTestActions
{
    public async Task ValidateProviderPayRequestNotSent(int examId, int evaluationId, long memberPlanId)
    {
        // Validate DB
        var expectedStatuses = new List<int>
        {
            ExamStatusCodes.ExamPerformed.ExamStatusCodeId,
            ExamStatusCodes.LabResultsReceived.ExamStatusCodeId,
            ExamStatusCodes.CdiPassedReceived.ExamStatusCodeId,
            ExamStatusCodes.ProviderNonPayableEventReceived.ExamStatusCodeId
        };
        await ValidateExamStatusCodesByEvaluationId(evaluationId, expectedStatuses);

        this.Invoking(async t=> await GetProviderPayByExamId(examId))
            .Should().ThrowAsync<ProviderPayNotFoundException>().GetAwaiter().GetResult()
            .WithMessage($"ProviderPay not found for ExamId {examId}");
        
        // Validate Kafka
        var providerNonPayableEventReceived = await CoreKafkaActions.GetUacrProviderNonPayableEventReceivedEvent<ProviderNonPayableEventReceived>(evaluationId);
        // providerNonPayableEventReceived.BillingProductCode.Should().Be(TestConstants.Product);
        providerNonPayableEventReceived.ProductCode.Should().Be(TestConstants.Product);
        // providerNonPayableEventReceived.PdfDeliveryDate.Should().BeSameDateAs(pdfDelivered.DeliveryDateTime);
        providerNonPayableEventReceived.ProviderId.Should().Be(TestConstants.Provider.ProviderId);
        providerNonPayableEventReceived.MemberPlanId.Should().Be(memberPlanId);
    }
    
    public async Task ValidateProviderPayRequestSent(int examId, int evaluationId, long memberPlanId)
    {
        // Validate DB
        var expectedStatuses = new List<int>
        {
            ExamStatusCodes.ExamPerformed.ExamStatusCodeId,
            ExamStatusCodes.LabResultsReceived.ExamStatusCodeId,
            ExamStatusCodes.ProviderPayableEventReceived.ExamStatusCodeId,
            ExamStatusCodes.ProviderPayRequestSent.ExamStatusCodeId,
            ExamStatusCodes.CdiPassedReceived.ExamStatusCodeId
        };
        await ValidateExamStatusCodesByEvaluationId(evaluationId, expectedStatuses);
        
        var providerPay = await GetProviderPayByExamId(examId);
        providerPay.PaymentId.Should().NotBeEmpty();
        
        // Validate Kafka
        var providerPayRequestSent = await CoreKafkaActions.GetUacrProviderPayRequestSentEvent<ProviderPayRequestSent>(evaluationId);
        // providerPayRequestSent.PdfDeliveryDate.Should().BeSameDateAs(pdfDelivered.DeliveryDateTime);
        // providerPayRequestSent.BillId.Should().NotBeEmpty();
        // providerPayRequestSent.BillingProductCode.Should().Be(TestConstants.Product);
        providerPayRequestSent.ProductCode.Should().Be(TestConstants.Product);
        providerPayRequestSent.ProviderId.Should().Be(TestConstants.Provider.ProviderId);
        providerPayRequestSent.MemberPlanId.Should().Be(memberPlanId);

    }
}