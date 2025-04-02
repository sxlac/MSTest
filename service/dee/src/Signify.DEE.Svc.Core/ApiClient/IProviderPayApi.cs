using Refit;
using Signify.DEE.Svc.Core.ApiClient.Requests;
using Signify.DEE.Svc.Core.ApiClient.Responses;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.ApiClient;

public interface IProviderPayApi
{
    [Post("/payments")]
    [Headers("Authorization: Bearer")]
    Task<IApiResponse<ProviderPayApiResponse>> SendProviderPayRequest(ProviderPayApiRequest providerPayApiRequest);
}