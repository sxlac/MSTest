using System.Threading.Tasks;
using Refit;
using Signify.FOBT.Svc.Core.ApiClient.Response;

namespace Signify.FOBT.Svc.Core.ApiClient;

/// <summary>
/// Interface to make requests to the Signify Provider core API. This API is for viewing providers, their
/// demographic data, and what plans they are credentialed for.
///
/// See https://chgit.censeohealth.com/projects/PROV2/repos/providerapiv2/browse/src/Signify.ProviderV2.WebApi/Controllers/ProviderController.cs
/// </summary>
public interface IProviderApi
{
	/// <summary>
	/// Get <see cref="ProviderInfoRs"/> from ProviderApi
	/// </summary>
	/// <param name="providerId">Identifier of the provider</param>
	/// <returns><see cref="ProviderInfoRs"/></returns>
	/// <remarks>https://developer.signifyhealth.com/catalog/default/api/provider/definition#/Provider/get_Providers__providerId_</remarks>
	[Get("/Providers/{providerId}")]
	[Headers("Authorization: Bearer")]
	Task<IApiResponse<ProviderInfoRs>> GetProviderById(int providerId);
}