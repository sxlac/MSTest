using System.Diagnostics.CodeAnalysis;
using Signify.Spirometry.Core.ApiClients.CdiApi.Flags.Requests;

namespace Signify.Spirometry.Core.ApiClients.CdiApi.Flags.Responses;

/// <remarks>
/// Corresponds to the model defined at:
/// https://dev.azure.com/signifyhealth/HCC/_git/cdi?path=/legacy/api/cdiapi/src/CH.CDI.Core/Models/SystemFlag.cs
/// </remarks>
[ExcludeFromCodeCoverage]
public class CdiSystemFlag : PendingCdiSystemFlag
{
    /// <summary>
    /// Identifier of the flag within the context/domain of CDI
    /// </summary>
    public int FlagId { get; set; }
}