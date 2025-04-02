using Signify.DEE.Svc.Core.Messages.Models;
using System;

namespace Signify.DEE.Svc.Core.Tests.Mocks;

public static class ExamStatusModelMock
{
    private static readonly FakeApplicationTime ApplicationTime = new();

    public static ExamStatusModel BuildExamStatusModel()
    {
        return new ExamStatusModel
        {
            ExamId = 1,
            CreatedDateTime = ApplicationTime.UtcNow(),
            ReceivedDateTime = ApplicationTime.UtcNow(),
            Status = "U",
            DeeEventId = Guid.NewGuid()
        };
    }
}