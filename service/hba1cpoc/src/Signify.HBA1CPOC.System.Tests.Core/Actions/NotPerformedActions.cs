using CH.Evaluation.Events;
using FluentAssertions;
using FluentAssertions.Common;
using Signify.HBA1CPOC.Messages.Events.Status;
using Signify.HBA1CPOC.System.Tests.Core.Constants;
using Signify.HBA1CPOC.System.Tests.Core.Models.Database;
using NotPerformedReason = Signify.HBA1CPOC.System.Tests.Core.Models.Database.NotPerformedReason;

namespace Signify.HBA1CPOC.System.Tests.Core.Actions;

public class NotPerformedActions: BaseTestActions
{
    protected NotPerformedReason GetNotPerformedReasonByEvaluationId(int evaluationId)
    {
        var exam = GetHba1CpocRecordByEvaluationId(evaluationId);
        var notPerformedRecord = GetNotPerformedRecordByExamId(exam.HBA1CPOCId);
        return GetNotPerformedReasonById(notPerformedRecord.NotPerformedReasonId);
    }

    protected HBA1CPOCNotPerformed GetNotPerformedByEvaluationId(int evaluationId)
    {
        var exam = GetHba1CpocRecordByEvaluationId(evaluationId);
        return GetNotPerformedRecordByExamId(exam.HBA1CPOCId);
    }
    
    protected async Task Validate_Database_Records_And_KafkaEvents(int evaluationId, Dictionary<int, string> answersDict, long? memberPlanId=null)
    {
        // Validate that the database HBA1CPOC details are as expected using EvaluationId in HBA1CPOC
        var notPerformedReason = GetNotPerformedReasonByEvaluationId(evaluationId);
        notPerformedReason.AnswerId.Should().Be(answersDict.Keys.Last());
        notPerformedReason.Reason.Should().Be(answersDict[answersDict.Keys.Last()]);

        // Validate the entry using EvaluationId in HBA1CPOC & HBA1CPOCStatus tables
        var notPerformed = GetNotPerformedByEvaluationId(evaluationId);
        var expectedStatusCodes = new List<int>
        {
            HBA1CPOCStatusCodes.HBA1CPOCNotPerformed.HBA1CPOCStatusCodeId,
            HBA1CPOCStatusCodes.BillRequestNotSent.HBA1CPOCStatusCodeId
        };
        ValidateExamStatusCodesByExamId(notPerformed.HBA1CPOCId,expectedStatusCodes);

        // Remove once  kafka validation in prod is enabled
        if (Environment.GetEnvironmentVariable("TEST_ENV").Equals("prod")) return;

        // # Validate that the Kafka events include the expected event headers
        var examStatusEvent = await CoreKafkaActions.GetA1CpocNotPerformedStatusEvent<NotPerformed>(evaluationId);
        examStatusEvent.Reason.Should().Be(answersDict[answersDict.Keys.Last()]);
        examStatusEvent.ReasonType.Should().Be(answersDict.Keys.Contains(Answers.ProviderReasonAnswerId)
            ? Answers.ProviderUnableAnswer
            : Answers.MemberRefusedAnswer);
        examStatusEvent.ReasonNotes.Should().Be(answersDict[answersDict.Keys.First()]);
        examStatusEvent.EvaluationId.Should().Be(evaluationId);
        examStatusEvent.MemberPlanId.Should().Be(memberPlanId);
        examStatusEvent.ProductCode.Should().Be("HBA1CPOC");

        var finalizedEvent = await CoreKafkaActions.GetEvaluationEvent<EvaluationFinalizedEvent>(evaluationId, "EvaluationFinalizedEvent");
        
        // Validate that the Kafka event details are as expected
        var billRequestNotSentEvent = await CoreKafkaActions.GetA1CpocBillRequestNotSentEvent<BillRequestNotSent>(evaluationId);
        billRequestNotSentEvent.BillingProductCode.Should().Be("HBA1CPOC");
        billRequestNotSentEvent.ProductCode.Should().Be("HBA1CPOC");
        billRequestNotSentEvent.EvaluationId.Should().Be(evaluationId);
        billRequestNotSentEvent.MemberPlanId.Should().Be(memberPlanId);
        billRequestNotSentEvent.CreateDate.Should().BeCloseTo(finalizedEvent.CreatedDateTime, TimeSpan.FromSeconds(5));
        billRequestNotSentEvent.ReceivedDate.Should().BeCloseTo(notPerformed.CreatedDateTime.ToDateTimeOffset(),TimeSpan.FromSeconds(5));
    }
}