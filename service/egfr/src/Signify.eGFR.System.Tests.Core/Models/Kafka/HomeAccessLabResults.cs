namespace Signify.eGFR.System.Tests.Core.Models.Kafka;

public class HomeAccessLabResults
{
    public string EstimatedGlomerularFiltrationRateResultColor { get; set; }
    public string EstimatedGlomerularFiltrationRateResultDescription { get; set; }
    public string EgfrResult { get; set; }
    public int EvaluationId { get; set; } 
    public DateTime DateLabReceived { get; set; } 
    
}