using System.Threading.Tasks;
using Refit;
using Signify.Spirometry.Core.ApiClients.ProviderApi.Responses;

namespace Signify.Spirometry.Core.ApiClients.ProviderApi
{
    /// <summary>
    /// Interface to make requests to the Signify Provider core API. This API is for viewing providers, their
    /// demographic data, and what plans they are credentialed for.
    ///
    /// See https://chgit.censeohealth.com/projects/PROV2/repos/providerapiv2/browse/src/Signify.ProviderV2.WebApi/Controllers/ProviderController.cs
    /// </summary>
    public interface IProviderApi
    {
        /// <summary>
        /// Get Provider
        /// </summary>
        /// <param name="providerId"></param>
        /// <returns></returns>
        [Get("/providers/{providerId}")]
        [Headers("Authorization: Bearer")]
        Task<ProviderInfo> GetProviderById(int providerId);
    }
}