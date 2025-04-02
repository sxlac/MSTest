using System.Threading.Tasks;
using Refit;
using Signify.uACR.Core.ApiClients.OktaApi.Responses;

namespace Signify.uACR.Core.ApiClients.OktaApi;

public interface IOktaApi
{
    [Post("/oauth2/default/v1/token")]
    Task<IApiResponse<AccessTokenResponse>> GetAccessToken([Header("Authorization")] string authorizationHeader,
        [Header("Content-Type")] string contentType,
        [Body] string body);
}