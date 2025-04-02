namespace Signify.uACR.System.Tests.Core.Models.Database;

public class BarcodeExam
{
    public int BarcodeExamId { get; set; }
    public int ExamId { get; set; }
    public string Barcode { get; set; }
    public DateTime CreatedDateTime { get; set; }
}