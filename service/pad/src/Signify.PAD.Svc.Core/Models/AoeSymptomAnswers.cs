using System.Diagnostics.CodeAnalysis;

namespace Signify.PAD.Svc.Core.Models;

[ExcludeFromCodeCoverage]
public sealed class AoeSymptomAnswers
{
    public int LateralityCodeId { get; set; }

    public int PedalPulseCodeId { get; set; }

    public bool FootPainDisappearsWalkingOrDangling { get; set; }

    public bool FootPainDisappearsOtc { get; set; }

    public bool HasSymptomsForAoeWithRestingLegPain { get; set; }

    public bool HasClinicalSupportForAoeWithRestingLegPain { get; set; }

    public bool AoeWithRestingLegPainConfirmed { get; set; }

    public string ReasonAoeWithRestingLegPainNotConfirmed { get; set; }
}
