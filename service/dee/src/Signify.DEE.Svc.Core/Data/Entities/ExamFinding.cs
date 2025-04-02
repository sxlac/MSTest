using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace Signify.DEE.Svc.Core.Data.Entities;

[ExcludeFromCodeCoverage]
public partial class ExamFinding
{
    public int ExamFindingId { get; set; }
    public int ExamResultId { get; set; }
    public int? LateralityCodeId { get; set; }
    public string Finding { get; set; }
    public string NormalityIndicator { get; set; }

    public virtual ExamResult ExamResult { get; set; }
    public virtual LateralityCode LateralityCode { get; set; }
}