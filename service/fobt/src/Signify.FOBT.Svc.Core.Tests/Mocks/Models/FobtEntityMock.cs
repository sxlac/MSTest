using System;
using Fobt = Signify.FOBT.Svc.Core.Data.Entities.FOBT;

namespace Signify.FOBT.Svc.Core.Tests.Mocks.Models;

public static class FobtEntityMock
{
    public static Fobt BuildFobt()
    {
        return new Fobt
        {
            FOBTId = 1234,
            EvaluationId = 123456,
            MemberId = 123,
            CenseoId = string.Empty,
            CreatedDateTime = DateTime.UtcNow.AddMinutes(-1),
            ReceivedDateTime = DateTime.UtcNow,
            Barcode = "01234567891234",
            ClientId = 12345
        };
    }

    public static Fobt BuildFobt(int evaluationId)
    {
        return new Fobt
        {
            FOBTId = 1234,
            EvaluationId = evaluationId,
            MemberId = 123,
            CenseoId = string.Empty,
            CreatedDateTime = DateTime.UtcNow.AddMinutes(-1),
            ReceivedDateTime = DateTime.UtcNow,
            Barcode = "01234567891234",
            ClientId = 12345
        };
    }

    public static Fobt BuildFobt(int evaluationId, string state)
    {
        return new Fobt
        {
            FOBTId = 1234,
            EvaluationId = evaluationId,
            MemberId = 123,
            CenseoId = string.Empty,
            CreatedDateTime = DateTime.UtcNow.AddMinutes(-1),
            ReceivedDateTime = DateTime.UtcNow,
            Barcode = "01234567891234",
            State = state,
            ClientId = 12345
        };
    }
}