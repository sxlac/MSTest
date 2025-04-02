namespace Signify.Spirometry.Svc.System.Tests.Core.Models.Database;

public class ExamStatus
{
    public int StatusCodeId { get; set; }
    public int SpirometryExamId { get; set; }
    public DateTime CreateDateTime { get; set; }
}