using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Signify.Spirometry.Core.ApiClients.CdiApi.Holds.Requests;

/// <summary>
/// Request body for sending a request to the Release Hold endpoint of the CDI Holds API
/// </summary>
[ExcludeFromCodeCoverage]
public class ReleaseHoldRequest
{
    /// <summary>
    /// Identifier of the application making the request to release the hold
    /// </summary>
    public string ApplicationId { get; set; }

    /// <summary>
    /// Product codes to release from the hold
    /// </summary>
    public IEnumerable<string> ProductCodes { get; set; }
}