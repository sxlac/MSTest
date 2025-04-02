using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.uACR.Core.Data.Entities;

[ExcludeFromCodeCoverage]
public class ExamStatus
{
    public int ExamStatusId { get; set; }
    public int ExamId { get; set; }
    public int ExamStatusCodeId { get; set; }
    public DateTimeOffset StatusDateTime { get; set; }
    public DateTimeOffset CreatedDateTime { get; set; }
    public virtual Exam Exam { get; set; }
    public virtual ExamStatusCode ExamStatusCode { get; set; }
}