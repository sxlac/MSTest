namespace Signify.PAD.Svc.System.Tests.Core.Models.Database;

public class AoeSymptomSupportResult
{
    public int PADId { get; set; }
    public int EvaluationId { get; set; }
    public string LeftScoreAnswerValue { get; set; }
    public string RightScoreAnswerValue { get; set; }
    public int FootPainRestingElevatedLateralityCodeId { get; set; }
    public bool FootPainDisappearsWalkingOrDangling { get; set; }
    public bool FootPainDisappearsOtc { get; set; }
    public int PedalPulseCodeId { get; set; }
    public bool AoeWithRestingLegPainConfirmed { get; set; }
    public bool HasClinicalSupportForAoeWithRestingLegPain { get; set; }
    
    public bool HasSymptomsForAoeWithRestingLegPain { get; set; }
    public string ReasonAoeWithRestingLegPainNotConfirmed { get; set; }
    public DateTime CreatedDateTime { get; set; }
}