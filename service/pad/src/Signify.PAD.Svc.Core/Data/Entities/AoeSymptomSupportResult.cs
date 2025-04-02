using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Signify.PAD.Svc.Core.Data.Entities;

[ExcludeFromCodeCoverage]
public class AoeSymptomSupportResult
{
    /// <summary>
    /// Identifier of the AoeSymptomSupportResult
    /// </summary>
    [Key]
    public int AoeSymptomSupportResultId { get; set; }

    /// <summary>
    /// Foreign key identifier of the corresponding <see cref=" PAD"/>
    /// </summary>
    public int PADId { get; set; }

    /// <summary>
    /// Foreign key identifier for foot pain value of the corresponding <see cref="LateralityCode"/>
    /// </summary>
    public short FootPainRestingElevatedLateralityCodeId { get; set; }

    /// <summary>
    /// Whether the foot pain disappears when walking around or hanging foot over edge
    /// </summary>
    public bool FootPainDisappearsWalkingOrDangling { get; set; }

    /// <summary>
    /// Whether the foot pain disappears when taking over the counter pain medication
    /// </summary>
    public bool FootPainDisappearsOtc { get; set; }

    /// <summary>
    /// Foreign key identifier of the corresponding <see cref=" PedalPulseCode"/>
    /// </summary>
    public short PedalPulseCodeId { get; set; }

    /// <summary>
    /// When the Pad PM received this result
    /// </summary>
    public DateTime CreatedDateTime { get; set; }

    /// <summary>
    /// Whether the member has been determined to have AoE with resting leg pain
    /// </summary>
    public bool? AoeWithRestingLegPainConfirmed { get; set; }

    /// <summary>
    /// Whether or not the members has symptoms for AoE with resting leg pain
    /// </summary>
    public bool? HasSymptomsForAoeWithRestingLegPain { get; set; }

    /// <summary>
    /// Whether the member has clinical support for AoE with resting leg pain
    /// </summary>
    public bool? HasClinicalSupportForAoeWithRestingLegPain { get; set; }

    /// <summary>
    /// Reason why the AoE with resting leg pain could not be confirmed
    /// </summary>
    public string ReasonAoeWithRestingLegPainNotConfirmed { get; set; }

    public virtual PAD PAD { get; set; }
    public virtual LateralityCode FootPainRestingElevatedLateralityCode { get; set; }
    public virtual PedalPulseCode PedalPulseCode { get; set;}
}
