using Signify.Spirometry.Svc.System.Tests.Core.Models.Kafka;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace Signify.Spirometry.Svc.System.Tests.Core.Actions;

public class NotPerformedActions : BaseTestActions
{
    protected async Task Validate_NotPerformed_Kafka_Database(int evaluationId, string reason, int reasonId, string reasonText, string testingNotes)
    {
        // Validate NotPerformed table in database
        var notPerformedReason = await GetNotPerformedRecordByEvaluationId(evaluationId);
        Assert.AreEqual(reasonId, notPerformedReason.AnswerId);
        Assert.AreEqual(reasonText, notPerformedReason.Reason);
        
        // Remove once  kafka validation in prod is enabled
        if (Environment.GetEnvironmentVariable("TEST_ENV")!.Equals("prod")) return;
        
        // Validate NotPerformed event in cecg_status topic 
        var examStatusEvent = await CoreKafkaActions.GetSpiroNotPerformedStatusEvent<NotPerformedEvent>(evaluationId);
        Assert.AreEqual(reasonText, examStatusEvent.Reason);
        Assert.AreEqual(reason, examStatusEvent.ReasonType);
        Assert.AreEqual(Provider.ProviderId, examStatusEvent.ProviderId);
        Assert.AreEqual(evaluationId, examStatusEvent.EvaluationId);
        Assert.AreEqual(testingNotes, examStatusEvent.ReasonNotes);
        Assert.AreEqual(Product, examStatusEvent.ProductCode);
        
    }
}