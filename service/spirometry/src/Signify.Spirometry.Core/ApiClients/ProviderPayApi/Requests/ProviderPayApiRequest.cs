using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Signify.Spirometry.Core.ApiClients.ProviderPayApi.Requests;

/// <summary>
/// Request to initiate provider payment 
/// </summary>
[ExcludeFromCodeCoverage]
public class ProviderPayApiRequest
{
#pragma warning disable CA1822
    public string ProviderProductCode => Constants.ProductCodes.Spirometry;
#pragma warning restore CA1822
    /// <summary>
    /// Identifier of the provider
    /// </summary>
    public long ProviderId { get; set; }

    /// <summary>
    /// CenseoId 
    /// </summary>
    public string PersonId { get; set; }

    /// <summary>
    /// Date of IHE
    /// </summary>
    public string DateOfService { get; set; }

    /// <summary>
    /// Identifier of the client (ie insurance company)
    /// </summary>
    public int ClientId { get; set; }

    /// <summary>
    /// A set of string key/value pairs that is specific to the product
    /// </summary>
    public Dictionary<string, string> AdditionalDetails { get; set; }
}