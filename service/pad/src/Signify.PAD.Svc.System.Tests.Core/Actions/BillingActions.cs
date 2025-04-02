using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Events;
using Signify.PAD.Svc.Core.Events.Status;
using Signify.PAD.Svc.System.Tests.Core.Constants;
using Signify.PAD.Svc.System.Tests.Core.Exceptions;

namespace Signify.PAD.Svc.System.Tests.Core.Actions;

public class BillingActions : BaseTestActions
{
    protected async Task ValidateBillRequestSent(PdfDeliveredToClient pdfEventValue, int evaluationId, int examId, long memberPlanId)
    {
        // Validate PDF Delivery in the DB
        var pdfDelivery = await GetPdfDeliveryByEvaluationId(evaluationId, 15, 2);

        Assert.AreEqual(pdfEventValue.BatchId, pdfDelivery.BatchId);
        Assert.AreEqual(pdfEventValue.BatchName, pdfDelivery.BatchName);
        Assert.AreEqual(pdfEventValue.EventId.ToString(), pdfDelivery.EventId);
        
        // ExamStatus
        var expectedStatusCodes = new List<int>
        {
            ExamStatusCodes.BillableEventReceived.PADStatusCodeId,
            ExamStatusCodes.BillRequestSent.PADStatusCodeId
        };
        
        await ValidateExamStatusCodesByExamId(examId,
            expectedStatusCodes, 10, 2);
        
        // Validate billing in the DB
        var billRecord = await GetBillingResultsByEvaluationId(evaluationId, 5, 2);
        Assert.IsFalse(string.IsNullOrEmpty(billRecord.BillId));
        Assert.IsNotNull(billRecord.AcceptedAt);
        
        // Validate BillRequestSent status in the Status Kafka Topic
        var billStatusEvent = await CoreKafkaActions.GetBillRequestSentEvent<BillRequestSent>(evaluationId);
        Assert.AreEqual(TestConstants.Product, billStatusEvent.ProductCode);
        Assert.AreEqual(Provider.ProviderId, billStatusEvent.ProviderId);
        Assert.AreEqual(memberPlanId, billStatusEvent.MemberPlanId);
        Assert.AreEqual(billRecord.BillId, billStatusEvent.BillId);
    }

    protected async Task ValidateBillRequestNotSent(int evaluationId, int examId, long memberPlanId)
    {
        // ExamStatus
        var expectedStatusCodes = new List<int>
        {
            ExamStatusCodes.BillRequestNotSent.PADStatusCodeId
        };
        
        await ValidateExamStatusCodesByExamId(examId,
            expectedStatusCodes, 5, 2);
        
        // BillRequest table
        await Assert.ThrowsExceptionAsync<BillRequestNotFoundException>(async () => await GetBillingResultsByEvaluationId(evaluationId, 5, 2),
            $"Record {evaluationId} not found in BillRequest table");
        
        // Validate BillRequestNotSent status in the Kafka Topic
        var billStatusEvent = await CoreKafkaActions.GetBillRequestNotSentEvent<BillRequestNotSent>(evaluationId);
        Assert.AreEqual(TestConstants.Product, billStatusEvent.ProductCode);
        Assert.AreEqual(Provider.ProviderId, billStatusEvent.ProviderId);
        Assert.AreEqual(memberPlanId, billStatusEvent.MemberPlanId);
    }
}
