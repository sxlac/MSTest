namespace Signify.HBA1CPOC.System.Tests.Core.Actions;

public class CancelledEvaluationActions : BaseTestActions
{
    protected void ValidateExamStatusCodesByEvaluationId(int evaluationId, List<int> statuses)
    {
        var exam = GetHba1CpocRecordByEvaluationId(evaluationId);
        ValidateExamStatusCodesByExamId(exam.HBA1CPOCId, statuses);
    }
}