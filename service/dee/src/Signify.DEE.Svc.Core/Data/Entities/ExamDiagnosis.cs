using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace Signify.DEE.Svc.Core.Data.Entities;

[ExcludeFromCodeCoverage]
public partial class ExamDiagnosis
{
    public int ExamDiagnosisId { get; set; }
    public int ExamResultId { get; set; }
    public string Diagnosis { get; set; }

    public virtual ExamResult ExamResult { get; set; }
}