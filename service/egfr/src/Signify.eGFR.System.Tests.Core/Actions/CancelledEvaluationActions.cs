using Signify.eGFR.System.Tests.Core.Models.Database;

namespace Signify.eGFR.System.Tests.Core.Actions;

public class CancelledEvaluationActions : BaseTestActions
{
    protected async Task<ProviderPay> GetProviderPayResultsWithEvalId(int evaluationId)
    {
        var exam = await GetExamByEvaluationId(evaluationId);
        return await GetProviderPayResultsByExamId(exam.ExamId);
    }
}