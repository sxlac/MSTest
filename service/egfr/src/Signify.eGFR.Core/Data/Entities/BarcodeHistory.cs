using System;

namespace Signify.eGFR.Core.Data.Entities;

public class BarcodeHistory
{
    public int BarcodeHistoryId { get; set; }
    public int ExamId { get; set; }
    public string Barcode { get; set; }
    //Foreign key
    public virtual Exam Exam { get; set; }
    public DateTimeOffset CreatedDateTime { get; set; }
}