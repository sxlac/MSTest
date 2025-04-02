using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Signify.DEE.Svc.Core.Data.Entities;

[ExcludeFromCodeCoverage]
public partial class ExamLateralityGrade
{
    public int ExamLateralityGradeId { get; set; }
    public int ExamId { get; set; }
    public int LateralityCodeId { get; set; }
    public bool Gradable { get; set; }

    public Exam Exam { get; set; }
    public virtual ICollection<NonGradableReason> NonGradableReasons { get; set; } = new HashSet<NonGradableReason>();
    public LateralityCode LateralityCode { get; set; }
}