using Signify.DEE.Svc.Core.Data.Entities;

namespace Signify.DEE.Svc.Core.Tests.Mocks;

public static class ExamImageEntityMock
{
    public static ExamImage BuildExamImage(int examImageId, int lateralityCodeId)
    {
        return new ExamImage
        {
            ExamImageId = examImageId,
            ExamId = 1,
            LateralityCodeId = lateralityCodeId,
            LateralityCode = lateralityCodeId == 1 ? LateralityCode.Right : LateralityCode.Left
        };
    }

    public static ExamImage BuildExamImage(int examImageId, int lateralityCodeId, string notGradableReasons, bool gradable)
    {
        return new ExamImage
        {
            ExamImageId = examImageId,
            ExamId = 1,
            LateralityCodeId = lateralityCodeId,
            LateralityCode = lateralityCodeId == 1 ? LateralityCode.Right : LateralityCode.Left,
            NotGradableReasons = notGradableReasons,
            Gradable = gradable
        };
    }

    public static ExamImage BuildExamImageFromServiceBus(int examImageId, string localId)
    {
        return new ExamImage
        {
            ExamImageId = examImageId,
            ExamId = 1,
            ImageLocalId = localId
        };
    }
}