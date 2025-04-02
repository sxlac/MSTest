using FobtNsbEvents;
using Signify.FOBT.Svc.Core.Events;
using System;

namespace Signify.FOBT.Svc.Core.Tests.Mocks.Models;

public static class FobtEvalReceivedMock
{
    public static FobtEvalReceived BuildFobtEvalReceived()
    {
        return new FobtEvalReceived
        {
            ApplicationId = "Signify.Evaluation.Service",
            AppointmentId = 1000084716,
            ClientId = 14,
            CreatedDateTime = DateTimeOffset.UtcNow,
            DateOfService = DateTime.UtcNow,
            DocumentPath = null,
            EvaluationId = 324357,
            EvaluationTypeId = 1,
            FormVersionId = 0,
            Location = new Location(32.925496267, 32.925496267),
            MemberId = 11990396,
            MemberPlanId = 21074285,
            ProviderId = 42879,
            ReceivedDateTime = DateTime.UtcNow,
            UserName = "vastest1",
            Products = [new Product("HHRA"), new Product("HBA1CPOC"), new Product("FOBT")]
        };
    }
}