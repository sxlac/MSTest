using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

#nullable disable

namespace Signify.DEE.Svc.Core.Data.Entities;

[ExcludeFromCodeCoverage]
public partial class LateralityCode
{
    public static readonly LateralityCode Right = new LateralityCode(1, "OD", "Right, Oculu");
    public static readonly LateralityCode Left = new LateralityCode(2, "OS", "Left, Oculus Sinster");
    public static readonly LateralityCode Both = new LateralityCode(3, "OU", "Both, Oculus Uterque");
    public static readonly LateralityCode Unknown = new LateralityCode(4, "Unknown", "Unknown");

    public LateralityCode()
    {
        ExamFindings = new HashSet<ExamFinding>();
        ExamImages = new HashSet<ExamImage>();
        ExamLateralityGrades = new HashSet<ExamLateralityGrade>();
    }

    public LateralityCode(int id, string name, string description)
    {
        LateralityCodeId = id;
        Name = name;
        Description = description;
    }

    public static readonly ImmutableList<LateralityCode> All = ImmutableList.Create(Right, Left, Both, Unknown);

    public static LateralityCode Create(string name)
    {
        return All.SingleOrDefault(x => name.Equals(x.Name, StringComparison.OrdinalIgnoreCase));
    }

    public int LateralityCodeId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public virtual ICollection<ExamFinding> ExamFindings { get; set; }
    public virtual ICollection<ExamImage> ExamImages { get; set; }
    public virtual ICollection<ExamLateralityGrade> ExamLateralityGrades { get; set; }
}