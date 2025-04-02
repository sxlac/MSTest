namespace Signify.eGFR.System.Tests.Core.Models.Database;

public class ExamStatusCode
{
    public int ExamStatusCodeId { get; set; }
    public string StatusName { get; set; }

    public ExamStatusCode(int id, string statusName)
    {
        ExamStatusCodeId = id;
        StatusName = StatusName;
    }
}