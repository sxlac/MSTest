namespace Signify.Spirometry.Svc.System.Tests.Core.Models.Database;

public class SpiroResults
{
    public int SpirometryExamId { get; set; }
    public int EvaluationId { get; set; }
    public string Normality { get; set; }
    public bool HasHistoryOfCopd { get; set; }
    public bool HasSmokedTobacco { get; set; }
    public int TotalYearsSmoking { get; set; }
    public bool ProducesSputumWithCough { get; set; }
    
    public int CoughMucusOccurrenceFrequencyId { get; set; }
    public int NoisyChestOccurrenceFrequencyId { get; set; }
    public int ShortnessOfBreathPhysicalActivityOccurrenceFrequencyId { get; set; }
    public int LungFunctionScore { get; set; }
    
    public string CoughMucusOccurrenceFrequencyValue { get; set; }
    public string NoisyChestOccurrenceFrequencyValue { get; set; }
    public string ShortnessOfBreathPAOccurrenceValue { get; set; }
    
    public string HadWheezingPast12moTrileanType { get; set; }
    public string GetsShortnessOfBreathAtRestTrileanType { get; set; }
    public string GetsShortnessOfBreathWithMildExertionTrileanType { get; set; }
    
}