using System.Threading.Tasks;
using Refit;
using Signify.Spirometry.Core.ApiClients.OktaApi.Responses;

namespace Signify.Spirometry.Core.ApiClients.OktaApi
{
    public interface IOktaApi
    {
        [Post("/oauth2/default/v1/token")]
        Task<ApiResponse<AccessTokenResponse>> GetAccessToken([Header("Authorization")] string authorizationHeader,
            [Header("Content-Type")] string contentType,
            [Body] string body);
    }
}