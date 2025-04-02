using System;
using System.Collections.Generic;

namespace Signify.DEE.Svc.Core.Messages.Models;

public class ExamResultModel
{
    public int ExamResultId { get; set; }
    public int ExamId { get; set; }
    public int PatientId { get; set; }
    public bool GradableImage { get; set; }
    public string CarePlan { get; set; }
    public DateTimeOffset? DateSigned { get; set; }
    public List<string> Diagnoses { get; set; } = new();
    public bool? LeftEyeHasPathology { get; set; }
    public bool LeftEyeGradable { get; set; }
    public bool? RightEyeHasPathology { get; set; }
    public bool RightEyeGradable { get; set; }
    public List<string> RightEyeFindings { get; set; } = new();
    public List<string> LeftEyeFindings { get; set; } = new();
    public ExamGraderModel Grader { get; set; }
}