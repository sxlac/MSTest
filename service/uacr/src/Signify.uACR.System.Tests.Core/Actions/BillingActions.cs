using FluentAssertions;
using Signify.Dps.Test.Utilities.Database.Exceptions;
using Signify.uACR.Core.Events.Status;
using Signify.uACR.System.Tests.Core.Constants;
using Signify.uACR.System.Tests.Core.Models.Database;

namespace Signify.uACR.System.Tests.Core.Actions;

public class BillingActions : BaseTestActions
{

    public async Task ValidateBillRequestNotSent(int examId, int evaluationId, PdfDeliveredToClient pdfDelivered, long memberPlanId)
    {
        // Validate DB
        var expectedStatuses = new List<int>
        {
            ExamStatusCodes.ExamPerformed.ExamStatusCodeId,
            ExamStatusCodes.LabResultsReceived.ExamStatusCodeId,
            ExamStatusCodes.BillRequestNotSent.ExamStatusCodeId,
            ExamStatusCodes.ClientPdfDelivered.ExamStatusCodeId
        };
        await ValidateExamStatusCodesByEvaluationId(evaluationId, expectedStatuses);

        this.Invoking(async t => await GetBillRequestByExamId(examId))
            .Should().ThrowAsync<BillRequestNotFoundException>().GetAwaiter().GetResult()
            .WithMessage($"BillRequest not found for ExamId {examId}");
        
        // Validate Kafka
        var billRequestNotSent = await CoreKafkaActions.GetUacrBillRequestNotSentEvent<BillRequestNotSent>(evaluationId);
        billRequestNotSent.BillingProductCode.Should().Be(TestConstants.Product);
        billRequestNotSent.ProductCode.Should().Be(TestConstants.Product);
        billRequestNotSent.PdfDeliveryDate.Should().BeSameDateAs(pdfDelivered.DeliveryDateTime);
        billRequestNotSent.ProviderId.Should().Be(TestConstants.Provider.ProviderId);
        billRequestNotSent.MemberPlanId.Should().Be(memberPlanId);
    }
    
    public async Task ValidateBillRequestSent(int examId, int evaluationId, PdfDeliveredToClient pdfDelivered, long memberPlanId)
    {
        // Validate DB
        var expectedStatuses = new List<int>
        {
            ExamStatusCodes.ExamPerformed.ExamStatusCodeId,
            ExamStatusCodes.LabResultsReceived.ExamStatusCodeId,
            ExamStatusCodes.BillRequestSent.ExamStatusCodeId,
            ExamStatusCodes.ClientPdfDelivered.ExamStatusCodeId,
            ExamStatusCodes.BillableEventReceived.ExamStatusCodeId
        };
        await ValidateExamStatusCodesByEvaluationId(evaluationId, expectedStatuses);
        
        var billRequest = await GetBillRequestByExamId(examId);
        billRequest.BillId.Should().NotBeEmpty();
        billRequest.AcceptedAt.Should().NotBe(null);
        
        // Validate Kafka
        var billRequestSent = await CoreKafkaActions.GetUacrBillRequestSentEvent<BillRequestSent>(evaluationId);
        billRequestSent.PdfDeliveryDate.Should().BeSameDateAs(pdfDelivered.DeliveryDateTime);
        billRequestSent.BillId.Should().NotBeEmpty();
        billRequestSent.BillingProductCode.Should().Be(TestConstants.Product);
        billRequestSent.ProductCode.Should().Be(TestConstants.Product);
        billRequestSent.ProviderId.Should().Be(TestConstants.Provider.ProviderId);
        billRequestSent.MemberPlanId.Should().Be(memberPlanId);

    }
}