using Signify.DEE.Svc.Core.Data.Entities;

namespace Signify.DEE.Svc.Core.Tests.Mocks;

public static class ExamStatusEntityMock
{
    private static readonly FakeApplicationTime ApplicationTime = new();

    public static ExamStatus BuildExamStatus(int examId, int examStatusCodeId)
    {
        return new ExamStatus
        {
            ExamStatusId = 1000,
            ExamId = examId,
            ExamStatusCodeId = examStatusCodeId,
            CreatedDateTime = ApplicationTime.UtcNow(),
            ReceivedDateTime = ApplicationTime.UtcNow()
        };
    }
}