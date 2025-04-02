using Signify.PAD.Svc.Core.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Signify.PAD.Svc.Core.Data.Entities;

[ExcludeFromCodeCoverage]
public class LateralityCode : IEntityEnum<LateralityCode>
{
    public static readonly LateralityCode Neither = new((int)LateralityCodes.Neither, "Neither");
    public static readonly LateralityCode Left = new((int)LateralityCodes.Left, "Left");
    public static readonly LateralityCode Right = new((int)LateralityCodes.Right, "Right");
    public static readonly LateralityCode Both = new((int)LateralityCodes.Both, "Both");

    private LateralityCode(short lateralityCodeId, string laterality)
    {
        LateralityCodeId = lateralityCodeId;
        Laterality = laterality;
    }

    /// <summary>
    /// Identifier of the laterality code
    /// </summary>
    [Key]
    public short LateralityCodeId { get; init; }

    /// <summary>
    /// Laterality response value
    /// </summary>
    public string Laterality { get; init; }

    public IEnumerable<LateralityCode> GetAllEnumerations()
        =>
            [
                Neither,
                Left,
                Right,
                Both
            ];

    public virtual ICollection<AoeSymptomSupportResult> AoeSymptomSupportResultFootPainRestingElevatedLateralityCodes { get; set; } = [];
}
