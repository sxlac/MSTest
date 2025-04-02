using Signify.FOBT.Svc.System.Tests.Core.Models.Database;
using FluentAssertions;
using Signify.FOBT.Svc.Core.Events;
using Signify.FOBT.Svc.System.Tests.Core.Constants;
using Signify.FOBT.Svc.System.Tests.Core.Models.Kafka;
using Signify.Dps.Test.Utilities.DataGen;

namespace Signify.FOBT.Svc.System.Tests.Core.Actions;

public class NotPerformedActions : BaseTestActions
{
    private readonly ProviderPayActions _providerPayActions = new ProviderPayActions();
    protected async Task<FOBTNotPerformed> GetNotPerformedRecordByEvaluationId(int evaluationId, int retryCount, int waitSeconds)
    {
        var exam = await GetFOBTByEvaluationId(evaluationId, retryCount, waitSeconds);
        return await GetNotPerformedRecordByExamId(exam.FOBTId, 2, 2);
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
            case Answers.TechnicalIssueAnswerId:
            case Answers.EnvironmentalIssueAnswerId:
            case Answers.NoSuppliesOrEquipmentAnswerId:
            case Answers.InsufficientTrainingAnswerId:
            case Answers.MemberPhysicallyUnableAnswerId:
                reasonType = answersDict[Answers.UnableToPerformAnswerId];
                reasonNotes = answersDict[Answers.UnableToPerformNotesAnswerId];
                break;
        }
        // Validate NotPerformed table in database
        var notPerformedRecord = await GetNotPerformedRecordByEvaluationId(evaluationId,15,2);
        notPerformedRecord.AnswerId.Should().Be(reasonAnswerId);
        notPerformedRecord.Notes.Should().Be(reasonNotes);
        
        var expectedIds = new List<int>
        {
            ExamStatusCodes.FOBTNotPerformed.FOBTStatusCodeId
        };
        await ValidateExamStatusCodesByEvaluationId(evaluationId, expectedIds, 2, 3);
        
        //Validate ProviderPay
        await _providerPayActions.ValidateProviderNonPayable(evaluationId);

        // Remove once  kafka validation in prod is enabled
        if (Environment.GetEnvironmentVariable("TEST_ENV").Equals("prod")) return;
        
        // Validate NotPerformed event in PAD_Status topic 
        var examStatusEvent = await CoreKafkaActions.GetNotPerformedStatusEvent<NotPerformedEvent>(evaluationId);
        examStatusEvent.Reason.Should().Be(answersDict[reasonAnswerId]);
        examStatusEvent.ReasonType.Should().Be(reasonType);
        examStatusEvent.ProviderId.Should().Be(Provider.ProviderId);
        examStatusEvent.EvaluationId.Should().Be(evaluationId);
        examStatusEvent.ReasonNotes.Should().Be(reasonNotes);
        examStatusEvent.ProductCode.Should().Be(TestConstants.Product);
        
        // Publish PdfDeliveredToClient event to pdfdelivery kafka topic
        var pdfDeliveryEvent = new PdfDeliveredToClient()
        {
            BatchId = 12345,
            BatchName = "FOBT_System_Tests" + DateTime.Now.Date.ToString("yyyyMMdd"),
            ProductCodes = new List<string>{"FOBT"},
            CreatedDateTime = DateTime.Now,
            DeliveryDateTime = DateTime.Now,
            EvaluationId = evaluationId,
            EventId = DataGen.NewGuid()
        };
        CoreKafkaActions.PublishPdfDeliveryEvent(pdfDeliveryEvent, evaluationId.ToString());
        await Task.Delay(5000);
        
        // Validate Exam NonBillable
        this.Invoking(async t => CoreKafkaActions.GetBillRequestSentEvent<BillRequestSentEvent>(evaluationId))
            .Should().ThrowAsync<NullReferenceException>();
    }
}
