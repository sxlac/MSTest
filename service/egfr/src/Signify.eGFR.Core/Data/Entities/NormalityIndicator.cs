using System.Collections.Generic;

namespace Signify.eGFR.Core.Data.Entities;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global - Virtual properties are used by EF
public class NormalityIndicator : IEntityEnum<NormalityIndicator>
{
    public static readonly NormalityIndicator Undetermined = new(1, "Undetermined", 'U');
    public static readonly NormalityIndicator Normal = new(2, "Normal", 'N');
    public static readonly NormalityIndicator Abnormal = new(3, "Abnormal", 'A');

    /// <inheritdoc />
    public IEnumerable<NormalityIndicator> GetAllEnumerations()
        => new[]
        {
            Undetermined,
            Normal,
            Abnormal
        };

    /// <summary>
    /// PK Identifier
    /// </summary>
    public int NormalityIndicatorId { get; init; }
    public string Normality { get; }
    public char Indicator { get; }

    private NormalityIndicator(int normalityIndicatorId, string normality, char indicator)
    {
        NormalityIndicatorId = normalityIndicatorId;
        Normality = normality;
        Indicator = indicator;
    }

    public virtual ICollection<LabResult> LabResults { get; set; }
}