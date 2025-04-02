namespace Signify.uACR.System.Tests.Core.Models.Kafka;

public class UacrLabResult
{
    public int EvaluationId { get; set; }
    public float CreatinineResult { get; set; }
    public string UrineAlbuminToCreatinineRatioResultColor { get; set; }
    public string UrineAlbuminToCreatinineRatioResultDescription { get; set; }
    public string UacrResult { get; set; }
    public string DateLabReceived { get; set; }
}