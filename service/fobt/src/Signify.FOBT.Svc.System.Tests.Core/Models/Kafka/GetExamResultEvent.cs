namespace Signify.FOBT.Svc.System.Tests.Core.Models.Kafka;

public class GetExamResultEvent
{
    public int EvaluationId { get; set; }
    public DateTime PerformedDate { get; set; }
    public DateTime ReceivedDate { get; set; }
    public DateTime MemberCollectionDate { get; set; }
    public string Determination { get; set; }
    public string Barcode { get; set; }
    public bool IsBillable { get; set; }
    public List<ResultData> Result { get; set; }
}

public class ResultData
{
    public string Result { get; set; }  // Property for Result.Result
    public string Exception { get; set; }  // Property for Result.Exception
    public string AbnormalIndicator { get; set; }
}