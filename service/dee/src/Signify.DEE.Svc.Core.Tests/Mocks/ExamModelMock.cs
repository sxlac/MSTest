using Signify.DEE.Svc.Core.Messages.Models;
using System;

namespace Signify.DEE.Svc.Core.Tests.Mocks;

public static class ExamModelMock
{
    private static readonly FakeApplicationTime ApplicationTime = new();

    public static ExamModel BuildExamModel()
    {
        return new ExamModel
        {
            ExamId = 1,
            EvaluationId = 300000,
            MemberPlanId = 200000,
            ProviderId = 1000,
            DateOfService = ApplicationTime.UtcNow(),
            CreatedDateTime = ApplicationTime.UtcNow(),
            State = "TX",
            RequestId = Guid.NewGuid(),
            ClientId = 14,
            AppointmentId = 12345,
            RetinalImageTestingNotes = "Retinal Image Testing Notes"
        };
    }
}