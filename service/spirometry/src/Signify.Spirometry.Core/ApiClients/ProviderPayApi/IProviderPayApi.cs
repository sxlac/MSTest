using System.Threading.Tasks;
using Refit;
using Signify.Spirometry.Core.ApiClients.ProviderPayApi.Requests;
using Signify.Spirometry.Core.ApiClients.ProviderPayApi.Responses;

namespace Signify.Spirometry.Core.ApiClients.ProviderPayApi;

public interface IProviderPayApi
{
    [Post("/payments")]
    [Headers("Authorization: Bearer")]
    Task<IApiResponse<ProviderPayApiResponse>> SendProviderPayRequest(ProviderPayApiRequest providerPayApiRequest);
}