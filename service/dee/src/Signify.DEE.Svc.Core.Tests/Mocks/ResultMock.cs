using Signify.DEE.Messages;
using Signify.DEE.Svc.Core.Constants;
using System.Collections.Generic;

namespace Signify.DEE.Svc.Core.Tests.Mocks;

public static class ResultMock
{
    private static readonly FakeApplicationTime ApplicationTime = new();

    public static Result BuildResultMock()
    {
        return new Result
        {
            ProductCode = ApplicationConstants.ProductCode,
            EvaluationId = 300000,
            PerformedDate = ApplicationTime.UtcNow(),
            ReceivedDate = ApplicationTime.UtcNow(),
            IsBillable = true,
            Determination = "N",
            Grader = new Grader
            {
                FirstName = "John",
                LastName = "Doe",
                NPI = "1234567890",
                Taxonomy = "207W00000X"
            },
            CarePlan = "Return in 6 months",
            DiagnosisCodes = new List<string>(),
            Results = new List<SideResultInfo>()
        };
    }
}