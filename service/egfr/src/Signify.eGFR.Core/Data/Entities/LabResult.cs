using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.eGFR.Core.Data.Entities;

[ExcludeFromCodeCoverage]
public class LabResult
{
    public int LabResultId { get; set; }
    public int ExamId { get; set; }
    public DateTimeOffset ReceivedDate { get; set; }
    public decimal? EgfrResult { get; set; }
    public int NormalityIndicatorId { get; set; }
    public string ResultDescription { get; set; }
    public DateTimeOffset CreatedDateTime { get; set; }

    public virtual Exam Exam { get; set; }
    public virtual NormalityIndicator NormalityIndicator { get; set; }
}