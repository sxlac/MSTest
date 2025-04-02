using System.Diagnostics.CodeAnalysis;


namespace Signify.DEE.Svc.Core.Data.Entities;

[ExcludeFromCodeCoverage]
public partial class NonGradableReason
{
    public int NonGradableReasonId { get; set; }
    public int ExamLateralityGradeId { get; set; }
    public string Reason { get; set; }
    public ExamLateralityGrade ExamLateralityGrade { get; set; }
}