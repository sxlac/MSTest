using Signify.eGFR.System.Tests.Core.Models.Database;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signify.eGFR.Core.Constants;
using Signify.eGFR.Core.Events.Akka;
using Signify.eGFR.System.Tests.Core.Constants;
using Signify.eGFR.System.Tests.Core.Models.Kafka;
using Signify.eGFR.System.Tests.Core.Models.NewRelic;

namespace Signify.eGFR.System.Tests.Core.Actions;

public class NotPerformedActions : BaseTestActions
{
 protected async Task<ExamNotPerformed> GetNotPerformedRecordByEvaluationId(int evaluationId)
    {
        var exam = await GetExamByEvaluationId(evaluationId);
        return await GetNotPerformedRecordByExamId(exam.ExamId);
        
    }
    
    protected async Task Validate_NotPerformed_Kafka_Database(int evaluationId, Dictionary<int, string> answersDict, int reasonAnswerId)
    {
        // Validate NotPerformed table in database
        var notPerformedRecord = await GetNotPerformedRecordByEvaluationId(evaluationId);

        var reasonType = "";
        var reasonNotes = answersDict[Answers.EgfrNotesAnswerId];
        switch (reasonAnswerId)
        {
            case Answers.MemberRecentlyCompletedAnswerId:
            case Answers.ScheduledToCompleteAnswerId:
            case Answers.MemberApprehensionAnswerId:
            case Answers.NotInterestedAnswerId:
                reasonType = answersDict[Answers.MemberRefusedAnswerId];
                notPerformedRecord.AnswerId.Should().Be(reasonAnswerId);
                break;
            case Answers.TechnicalIssueAnswerId :
            case Answers.EnvironmentalIssueAnswerId :
            case Answers.NoSuppliesOrEquipmentAnswerId :
            case Answers.InsufficientTrainingAnswerId :
            case Answers.MemberPhysicallyUnableAnswerId :
                reasonType = answersDict[Answers.UnableToPerformAnswerId];
                notPerformedRecord.AnswerId.Should().Be(reasonAnswerId);
                break;
            case Answers.ClinicallyIrrelevantAnswerId:
                notPerformedRecord.AnswerId.Should().Be(reasonAnswerId);
                reasonType = answersDict[Answers.ClinicallyIrrelevantAnswerId];
                break;
            case Answers.KedMemberRecentlyCompletedAnswerId:
            case Answers.KedScheduledToCompleteAnswerId:
            case Answers.KedMemberApprehensionAnswerId:
            case Answers.KedNotInterestedAnswerId:
                notPerformedRecord.AnswerId.Should().Be(ReasonAnswerIdMappings[reasonAnswerId]);
                reasonType = answersDict[Answers.KedMemberRefusedAnswerId];
                break;
            case Answers.KedTechnicalIssueAnswerId :
            case Answers.KedEnvironmentalIssueAnswerId :
            case Answers.KedNoSuppliesOrEquipmentAnswerId :
            case Answers.KedInsufficientTrainingAnswerId :
            case Answers.KedMemberPhysicallyUnableAnswerId :
                notPerformedRecord.AnswerId.Should().Be(ReasonAnswerIdMappings[reasonAnswerId]);
                reasonType = answersDict[Answers.KedUnableToPerformAnswerId];
                break;
        }
        
        notPerformedRecord.Notes.Should().Be(reasonNotes);
        
        var expectedIds = new List<int>
        {
            ExamStatusCodes.ExamNotPerformed.ExamStatusCodeId
        };
        await ValidateExamStatusCodesByEvaluationId(evaluationId, expectedIds);

        // Remove once  kafka validation in prod is enabled
        if (Environment.GetEnvironmentVariable("TEST_ENV").Equals("prod")) return;
        
        
        var finalizedEvent =
            CoreKafkaActions.GetEvaluationEvent<EvaluationFinalizedEvent>(evaluationId,
                "EvaluationFinalizedEvent");
        
        // Validate NotPerformed event in egfr_status topic 
        var examStatusEvent = await CoreKafkaActions.GetEgfrNotPerformedStatusEvent<NotPerformedEvent>(evaluationId);
        examStatusEvent.Reason.Should().Be(answersDict[reasonAnswerId]);
        examStatusEvent.ReasonType.Should().Be(reasonType);
        examStatusEvent.ProviderId.Should().Be(Provider.ProviderId);
        examStatusEvent.EvaluationId.Should().Be(evaluationId);
        // examStatusEvent.MemberPlanId.Should().Be(memberplanId);
        examStatusEvent.ReasonNotes.Should().Be(reasonNotes);
        examStatusEvent.ProductCode.Should().Be(TestConstants.Product);
        // examStatusEvent.CreatedDate.Should().Be(finalizedEvent.CreatedDateTime);
        // examStatusEvent.ReceivedDate.Should().Be(finalizedEvent.ReceivedDateTime);
    }
    
    protected async Task Validate_NR_Event(int evaluationId, string reason)
    {
        var nREvent =
            await NewRelicActions.GetCustomEvent<EvaluationNotPerformedEvent>(Observability.Evaluation.EvaluationNotPerformedEvent, 
                new Dictionary<string, string>{{"EvaluationId", evaluationId.ToString()}, {"appName", "egfr"}});
        Assert.AreEqual(nREvent.First().NotPerformedReason, reason);
        
    }
}