using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.Spirometry.Core.Data.Entities;

/// <summary>
/// Details of a spirometry exam overread. These results come from NuvoAir (https://www.nuvoair.com/).
/// </summary>
[ExcludeFromCodeCoverage]
// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global - Virtual properties are used by EF
public class OverreadResult
{
    /// <summary>
    /// Identifier of this overread result
    /// </summary>
    public int OverreadResultId { get; set; }
    /// <summary>
    /// Vendor's identifier of this overread result
    /// </summary>
    public Guid ExternalId { get; set; }
    /// <summary>
    /// Identifier of the in-home appointment with the member
    /// </summary>
    public long AppointmentId { get; set; }
    /// <summary>
    /// Identifier of the session within the AirMD application this overread corresponds to
    /// </summary>
    public Guid SessionId { get; set; }
    /// <summary>
    /// FEV-1/FVC ratio the pulmonologist determined for these results
    /// </summary>
    public decimal Fev1FvcRatio { get; set; }
    /// <summary>
    /// Overall normality (indicator) of the overread (ie normality of FEV-1/FVC)
    /// as determined by the pulmonologist. 
    /// </summary>
    public short NormalityIndicatorId { get; set; }
    /// <summary>
    /// When the spirometry test was performed
    /// </summary>
    public DateTime PerformedDateTime { get; set; }
    /// <summary>
    /// When this overread was created by the pulmonologist
    /// </summary>
    public DateTime OverreadDateTime { get; set; }
    /// <summary>
    /// Name of the pulmonologist
    /// </summary>
    public string OverreadBy { get; set; }
    /// <summary>
    /// Free-form comment from the pulmonologist about the overread
    /// </summary>
    public string OverreadComment { get; set; }
    /// <summary>
    /// Identifier of the best quality spirometry test conducted in the session
    /// </summary>
    public Guid? BestTestId { get; set; }
    /// <summary>
    /// Free-form comment from the pulmonologist about the best FVC test
    /// </summary>
    public string BestFvcTestComment { get; set; }
    /// <summary>
    /// Identifier of the spirometry test conducted in the session with the best FVC 
    /// </summary>
    public Guid? BestFvcTestId { get; set; }
    /// <summary>
    /// Free-form comment from the pulmonologist about the best FEV-1 test
    /// </summary>
    public string BestFev1TestComment { get; set; }
    /// <summary>
    /// Identifier of the spirometry test conducted in the session with the best FEV-1
    /// </summary>
    public Guid? BestFev1TestId { get; set; }
    /// <summary>
    /// Free-form comment from the pulmonologist about the best PEF (Peak Expiratory Flow) test
    /// </summary>
    public string BestPefTestComment { get; set; }
    /// <summary>
    /// Identifier of the spirometry test conducted in the session with the best PEF (Peak Expiratory Flow)
    /// </summary>
    public Guid? BestPefTestId { get; set; }
    /// <summary>
    /// When this overread was received by Signify
    /// </summary>
    public DateTime ReceivedDateTime { get; set; }
    /// <summary>
    /// When this overread was saved to the spirometry database
    /// </summary>
    public DateTime CreatedDateTime { get; set; }

    public virtual NormalityIndicator NormalityIndicator { get; set; }
}