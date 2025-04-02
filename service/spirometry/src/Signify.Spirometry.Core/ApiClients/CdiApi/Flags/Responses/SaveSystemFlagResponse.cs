using System.Diagnostics.CodeAnalysis;

namespace Signify.Spirometry.Core.ApiClients.CdiApi.Flags.Responses;

/// <remarks>
/// Corresponds to the model defined at:
/// https://dev.azure.com/signifyhealth/HCC/_git/cdi?path=/legacy/api/cdiapi/src/CH.CDI.WebApi/Representations/SaveSystemFlagResponse.cs
/// </remarks>
[ExcludeFromCodeCoverage]
public class SaveSystemFlagResponse
{
    /// <summary>
    /// Flag that was created
    /// </summary>
    public CdiSystemFlag Flag { get; set; }
}