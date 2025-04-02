using Signify.FOBT.Svc.System.Tests.Core.Models.Database;

namespace Signify.FOBT.Svc.System.Tests.Core.Actions;

public class CancelledEvaluationActions: BaseTestActions
{
    protected async Task<ProviderPay> GetProviderPayResultsWithEvalId(int evaluationId, int retryCount, int waitSeconds)
    {
        var exam = await GetFOBTByEvaluationId(evaluationId, retryCount, waitSeconds);
        return await GetProviderPayResultsByExamId(exam.FOBTId, retryCount, waitSeconds);
    }
}