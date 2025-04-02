
using Signify.PAD.Svc.System.Tests.Core.Models.Database;

namespace Signify.PAD.Svc.System.Tests.Core.Actions;

public class CancelledEvaluationActions : BaseTestActions
{
    protected async Task<ProviderPay> GetProviderPayResultsWithEvalId(int evaluationId, int retryCount, int waitSeconds)
    {
        var exam = await GetPadByEvaluationId(evaluationId, retryCount, waitSeconds);
        return await GetProviderPayResultsByExamId(exam.PADId, retryCount, waitSeconds);
    }
}