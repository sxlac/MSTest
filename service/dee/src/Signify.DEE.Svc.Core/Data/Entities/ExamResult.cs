using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace Signify.DEE.Svc.Core.Data.Entities;

[ExcludeFromCodeCoverage]
public partial class ExamResult
{
    public ExamResult()
    {
        ExamDiagnoses = new HashSet<ExamDiagnosis>();
        ExamFindings = new HashSet<ExamFinding>();
    }

    public int ExamResultId { get; set; }
    public int ExamId { get; set; }
    public bool GradableImage { get; set; }
    public string GraderFirstName { get; set; }
    public string GraderLastName { get; set; }
    public string GraderNpi { get; set; }
    public string GraderTaxonomy { get; set; }
    public DateTimeOffset? DateSigned { get; set; }
    public string CarePlan { get; set; }
    public virtual Exam Exam { get; set; }
    public virtual ICollection<ExamDiagnosis> ExamDiagnoses { get; set; }
    public virtual ICollection<ExamFinding> ExamFindings { get; set; }
    public string NormalityIndicator { get; set; }
    public bool? LeftEyeHasPathology { get; set; }
    public bool? RightEyeHasPathology { get; set; }
}