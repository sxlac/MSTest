using System.Threading.Tasks;
using Refit;
using Signify.eGFR.Core.ApiClients.ProviderApi.Responses;

namespace Signify.eGFR.Core.ApiClients.ProviderApi;

/// <summary>
/// Interface to make requests to the Signify Provider core API. This API is for viewing providers, their
/// demographic data, and what plans they are credentialed for.
///
/// See https://chgit.censeohealth.com/projects/PROV2/repos/providerapiv2/browse/src/Signify.ProviderV2.WebApi/Controllers/ProviderController.cs
/// </summary>
public interface IProviderApi
{
    /// <summary>
    /// Get <see cref="ProviderInfo"/> from ProviderApi
    /// </summary>
    /// <param name="providerId">Identifier of the provider</param>
    /// <returns><see cref="ProviderInfo"/></returns>
    /// <remarks>https://developer.signifyhealth.com/catalog/default/api/provider/definition#/Provider/get_Providers__providerId_</remarks>
    [Get("/providers/{providerId}")]
    [Headers("Authorization: Bearer")]
    Task<ProviderInfo> GetProviderById(int providerId);
}