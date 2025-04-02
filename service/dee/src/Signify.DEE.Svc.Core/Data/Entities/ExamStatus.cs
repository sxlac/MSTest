using System;
using System.Diagnostics.CodeAnalysis;

#nullable disable
namespace Signify.DEE.Svc.Core.Data.Entities;

[ExcludeFromCodeCoverage]
public partial class ExamStatus
{
    public int ExamStatusId { get; set; }
    public int ExamId { get; set; }
    public DateTimeOffset CreatedDateTime { get; set; }
    public DateTimeOffset ReceivedDateTime { get; set; }
    public int ExamStatusCodeId { get; set; }
    public Guid? DeeEventId { get; set; }
    public virtual Exam Exam { get; set; }
    public virtual ExamStatusCode ExamStatusCode { get; set; }

    public static ExamStatus Create(ExamStatusCode statusCode, DateTimeOffset createdDate, Guid? deeEventId = null)
    {
        var status = new ExamStatus
        {
            CreatedDateTime = createdDate,
            DeeEventId = deeEventId,
            ExamStatusCode = statusCode
        };
        return status;
    }
}