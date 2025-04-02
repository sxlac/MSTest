using Refit;
using Signify.HBA1CPOC.Svc.Core.ApiClient.Requests;
using Signify.HBA1CPOC.Svc.Core.ApiClient.Response;
using System.Threading.Tasks;

namespace Signify.HBA1CPOC.Svc.Core.ApiClient;

public interface IProviderPayApi
{
    [Post("/payments")]
    [Headers("Authorization: Bearer")]
    Task<IApiResponse<ProviderPayApiResponse>> SendProviderPayRequest(ProviderPayApiRequest providerPayApiRequest);
}