using Signify.PAD.Svc.System.Tests.Core.Models.Database;
using FluentAssertions;
using Signify.PAD.Svc.Core.Events;
using Signify.PAD.Svc.System.Tests.Core.Constants;
using Signify.PAD.Svc.System.Tests.Core.Models.Kafka;

namespace Signify.PAD.Svc.System.Tests.Core.Actions;

public class NotPerformedActions : BaseTestActions
{
    protected async Task<NotPerformedReason> GetNotPerformedReasonByEvaluationId(int evaluationId, int retryCount, int waitSeconds)
    {
        var exam = await GetPadByEvaluationId(evaluationId, retryCount, waitSeconds);
        return await GetNotPerformedRecordByExamId(exam.PADId, 2, 2);
        
    }
    
    protected async Task Validate_NotPerformed_Kafka_Database(int evaluationId, Dictionary<int, string> answersDict, int reasonAnswerId)
    {
        var reasonType = "";
        var reasonNotes = "";
        switch (reasonAnswerId)
        {
            case Answers.MemberRecentlyCompletedAnswerId:
            case Answers.ScheduledToCompleteAnswerId:
            case Answers.MemberApprehensionAnswerId:
            case Answers.NotInterestedAnswerId:
            case Answers.OtherAnswerId:
                reasonType = answersDict[Answers.MemberRefusedAnswerId];
                reasonNotes = answersDict[Answers.MemberRefusalNotesAnswerId];
                break;
            case Answers.TechnicalIssueAnswerId :
            case Answers.EnvironmentalIssueAnswerId :
            case Answers.NoSuppliesOrEquipmentAnswerId :
            case Answers.InsufficientTrainingAnswerId :
            case Answers.MemberPhysicallyUnableAnswerId :
                reasonType = answersDict[Answers.UnableToPerformAnswerId];
                reasonNotes = answersDict[Answers.UnableToPerformNotesAnswerId];
                break;
            case Answers.ClinicallyIrrelevantAnswerId:
                reasonType = answersDict[Answers.ClinicallyIrrelevantAnswerId];
                reasonNotes = answersDict[Answers.ClinicallyIrrelevantReasonAnswerId];
                break;
        }
        // Validate NotPerformed table in database
        var notPerformedReason = await GetNotPerformedReasonByEvaluationId(evaluationId,15,2);
        notPerformedReason.AnswerId.Should().Be(reasonAnswerId);
        notPerformedReason.Notes.Should().Be(reasonNotes);
        
        var expectedIds = new List<int>
        {
            ExamStatusCodes.ExamNotPerformed.PADStatusCodeId
        };
        await ValidateExamStatusCodesByEvaluationId(evaluationId, expectedIds, 2, 3);

        // Remove once  kafka validation in prod is enabled
        if (Environment.GetEnvironmentVariable("TEST_ENV").Equals("prod")) return;
        
        
        var finalizedEvent =
            await CoreKafkaActions.GetEvaluationEvent<EvaluationFinalizedEvent>(evaluationId,
                "EvaluationFinalizedEvent");
        
        // Validate NotPerformed event in PAD_Status topic 
        var examStatusEvent = await CoreKafkaActions.GetNotPerformedStatusEvent<NotPerformedEvent>(evaluationId);
        examStatusEvent.Reason.Should().Be(answersDict[reasonAnswerId]);
        examStatusEvent.ReasonType.Should().Be(reasonType);
        examStatusEvent.ProviderId.Should().Be(Provider.ProviderId);
        examStatusEvent.EvaluationId.Should().Be(evaluationId);
        examStatusEvent.ReasonNotes.Should().Be(reasonNotes);
        examStatusEvent.ProductCode.Should().Be(TestConstants.Product);
        examStatusEvent.CreateDate.Should().Be(finalizedEvent.CreatedDateTime);
        examStatusEvent.ReceivedDate.Should().Be(finalizedEvent.ReceivedDateTime);
    }
}