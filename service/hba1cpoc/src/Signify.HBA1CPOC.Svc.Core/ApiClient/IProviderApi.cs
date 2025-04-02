using Refit;
using Signify.HBA1CPOC.Svc.Core.ApiClient.Response;
using System.Threading.Tasks;

namespace Signify.HBA1CPOC.Svc.Core.ApiClient;

public interface IProviderApi
{
    [Get("/Providers/{providerId}")]
    [Headers("Authorization: Bearer")]
    Task<IApiResponse<ProviderRs>> GetProviderById(int providerId);
}