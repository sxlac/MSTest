using Signify.DEE.Messages.Status;
using Signify.DEE.Svc.Core.Constants;

namespace Signify.DEE.Svc.Core.Tests.Mocks;

public static class ResultsReceivedMock
{
    private static readonly FakeApplicationTime ApplicationTime = new();

    public static ResultsReceived BuildResultsReceived()
    {
        return new ResultsReceived
        {
            ProductCode = ApplicationConstants.ProductCode,
            EvaluationId = 300000,
            MemberPlanId = 200000,
            ProviderId = 1000,
            CreateDate = ApplicationTime.UtcNow(),
            ReceivedDate = ApplicationTime.UtcNow()
        };
    }
}