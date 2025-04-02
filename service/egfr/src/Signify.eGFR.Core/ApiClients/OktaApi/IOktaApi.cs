using System.Threading.Tasks;
using Refit;
using Signify.eGFR.Core.ApiClients.OktaApi.Responses;

namespace Signify.eGFR.Core.ApiClients.OktaApi;

public interface IOktaApi
{
    [Post("/oauth2/default/v1/token")]
    Task<IApiResponse<AccessTokenResponse>> GetAccessToken([Header("Authorization")] string authorizationHeader,
        [Header("Content-Type")] string contentType,
        [Body] string body);
}