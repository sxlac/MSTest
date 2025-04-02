namespace Signify.eGFR.Core.ApiClients.ProviderApi.Responses;

/// <summary>
/// Details of a provider that performed an evaluation. This is a subset of the full member model, which can be found at
/// https://chgit.censeohealth.com/projects/PROV2/repos/providerapiv2/browse/src/Signify.ProviderV2.WebApi/Responses/ProviderResponse.cs
/// </summary>
public class ProviderInfo
{
    /// <summary>
    /// Identifier of the provider within Signify
    /// </summary>
    public int ProviderId { get; set; }

    public string FirstName { get; set; }
    public string LastName { get; set; }

    /// <summary>
    /// The NPI is a HIPAA Administrative Simplification Standard. This is a unique 10-digit identification
    /// number for covered health care providers.
    ///
    /// For more information, see
    /// https://www.cms.gov/Regulations-and-Guidance/Administrative-Simplification/NationalProvIdentStand
    /// </summary>
    public string NationalProviderIdentifier { get; set; }
}