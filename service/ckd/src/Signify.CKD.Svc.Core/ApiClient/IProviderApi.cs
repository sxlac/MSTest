using System.Threading.Tasks;
using Refit;
using Signify.CKD.Svc.Core.ApiClient.Response;

namespace Signify.CKD.Svc.Core.ApiClient
{
    public interface IProviderApi
    {
        [Get("/Providers/{providerId}")]
        [Headers("Authorization: Bearer")]
        Task<ApiResponse<ProviderRs>> GetProviderById(int providerId);
    }
}
