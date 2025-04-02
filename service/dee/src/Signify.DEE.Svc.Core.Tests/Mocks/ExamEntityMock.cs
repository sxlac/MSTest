using Signify.DEE.Svc.Core.Data.Entities;
using System;

namespace Signify.DEE.Svc.Core.Tests.Mocks;

public static class ExamEntityMock
{
    private static readonly FakeApplicationTime ApplicationTime = new();
    public static Exam BuildExam()
    {
        return new Exam
        {
            ExamId = 1,
            EvaluationId = 300000,
            MemberPlanId = 200000,
            ProviderId = 1000,
            DateOfService = ApplicationTime.UtcNow(),
            Gradeable = null,
            CreatedDateTime = ApplicationTime.UtcNow(),
            State = "TX",
            RequestId = Guid.NewGuid(),
            ClientId = 14,
            ExamLocalId = "84",
            EvaluationObjectiveId = 1
        };
    }

    public static Exam BuildExam(int examId, int deeExamId, int evaluationId, int memberPlanId)
    {
        return new Exam
        {
            ExamId = examId,
            EvaluationId = evaluationId,
            MemberPlanId = memberPlanId,
            ProviderId = 1000,
            DateOfService = DateTime.UtcNow,
            Gradeable = null,
            CreatedDateTime = DateTime.UtcNow,
            State = "TX",
            RequestId = Guid.NewGuid(),
            ClientId = 14,
            ExamLocalId = "85",
            EvaluationObjectiveId = 1
        };
    }
}