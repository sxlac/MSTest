using Signify.FOBT.Svc.Core.Data.Entities;
using System;

namespace Signify.FOBT.Svc.Core.Tests.Mocks.Models;

public static class FobtStatusEntityMock
{
    public static FOBTStatus BuildFobtStatus(int evaluationId, FOBTStatusCode fobtStatusCode)
    {
        return new FOBTStatus
        {
            FOBTStatusId = 1,
            FOBTStatusCode = fobtStatusCode,
            FOBT = FobtEntityMock.BuildFobt(evaluationId),
            CreatedDateTime = DateTime.UtcNow.AddMinutes(-1)
        };
    }
}