using System.Diagnostics.CodeAnalysis;

namespace Signify.Spirometry.Core.ApiClients.CdiApi.Flags.Requests;

/// <remarks>
/// This corresponds to the model defined at:
/// https://dev.azure.com/signifyhealth/HCC/_git/cdi?path=/legacy/api/cdiapi/src/CH.CDI.WebApi/Models/SaveSystemFlagRequest.cs
/// </remarks>
[ExcludeFromCodeCoverage]
public class SaveSystemFlagRequest
{
    /// <summary>
    /// Identifier of the evaluation to be associated with this request
    /// </summary>
    public long EvaluationId { get; set; }

    /// <summary>
    /// Identifier of the Spirometry process manager
    /// </summary>
    public string ApplicationId { get; set; }

    /// <summary>
    /// Flag to create
    /// </summary>
    public PendingCdiSystemFlag SystemFlag { get; set; }
}