using System;

namespace Signify.PAD.Svc.Core.Converter
{
    public interface IWaveformDocumentConverter
    {
        int ConvertMemberPlanId(string fileName);
        DateTime ConvertDateOfExam(string fileName);
    }
}
