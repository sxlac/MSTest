using Signify.FOBT.Svc.Core.Events;
using System.Collections.Generic;
using System;

namespace Signify.FOBT.Svc.Core.Tests.Mocks.Models;

public static class EvaluationFinalizedEventMock
{
    public static EvaluationFinalizedEvent BuildEvaluationFinalizedEvent(List<Product> products)
    {
        return new EvaluationFinalizedEvent
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
            Products = products
        };
    }
}