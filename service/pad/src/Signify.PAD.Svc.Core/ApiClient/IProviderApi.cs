using System.Threading.Tasks;
using Refit;
using Signify.PAD.Svc.Core.ApiClient.Response;

namespace Signify.PAD.Svc.Core.ApiClient
{
    public interface IProviderApi
    {
        /// <summary>
        /// Get <see cref="ProviderRs"/> from ProviderApi
        /// </summary>
        /// <param name="providerId">Identifier of the provider</param>
        /// <returns><see cref="ProviderRs"/></returns>
        /// <remarks>https://developer.signifyhealth.com/catalog/default/api/provider/definition#/Provider/get_Providers__providerId_</remarks>
        [Get("/Providers/{providerId}")]
        [Headers("Authorization: Bearer")]
        Task<ProviderRs> GetProviderById(int providerId);
    }
}