using System.Threading.Tasks;
using Refit;
using Signify.A1C.Svc.Core.ApiClient.Response;

namespace Signify.A1C.Svc.Core.ApiClient
{
    public interface IOktaApi
    {
        [Post("/oauth2/default/v1/token")]
        Task<ApiResponse<AccessToken>> GetAccessToken([Header("Authorization")] string authorizationHeader, [Header("Content-Type")] string contentType, [Body] string request);
    }
}