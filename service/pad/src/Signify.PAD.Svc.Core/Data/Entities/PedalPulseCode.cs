using Signify.PAD.Svc.Core.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Signify.PAD.Svc.Core.Data.Entities;

public class PedalPulseCode : IEntityEnum<PedalPulseCode>
{
    public static readonly PedalPulseCode Normal = new((int)PedalPulseCodes.Normal, "Normal");
    public static readonly PedalPulseCode AbnormalLeft = new((int)PedalPulseCodes.AbnormalLeft, "Abnormal-Left");
    public static readonly PedalPulseCode AbnormalRight = new((int)PedalPulseCodes.AbnormalRight, "Abnormal-Right");
    public static readonly PedalPulseCode AbnormalBilateral = new((int)PedalPulseCodes.AbnormalBilateral, "Abnormal-Bilateral");
    public static readonly PedalPulseCode NotPerformed = new((int)PedalPulseCodes.NotPerformed, "Not Performed");

    private PedalPulseCode(short pedalPulseCodeId, string pedalPulse)
    {
        PedalPulseCodeId = pedalPulseCodeId;
        PedalPulse = pedalPulse;
    }

    /// <summary>
    /// Identifier of the Pedal Pulse Code
    /// </summary>
    [Key]
    public short PedalPulseCodeId { get; init; }

    /// <summary>
    /// Pedal Pulse response value
    /// </summary>
    public string PedalPulse { get; init; }

    public IEnumerable<PedalPulseCode> GetAllEnumerations()
        =>
            [
                Normal,
                AbnormalLeft,
                AbnormalRight,
                AbnormalBilateral,
                NotPerformed
            ];
}
