using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.uACR.Core.Data.Entities;

[ExcludeFromCodeCoverage]
public class BarcodeExam
{
    public int BarcodeExamId { get; set; }
    public int ExamId { get; set; }
    public string Barcode { get; set; }
    public DateTimeOffset CreatedDateTime { get; set; }

    public virtual Exam Exam { get; set; }
}