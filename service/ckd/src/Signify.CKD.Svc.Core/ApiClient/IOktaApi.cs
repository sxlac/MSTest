using System.Threading.Tasks;
using Refit;
using Signify.CKD.Svc.Core.ApiClient.Response;

namespace Signify.CKD.Svc.Core.ApiClient
{
    public interface IOktaApi
    {
        [Post("/oauth2/default/v1/token")]
        Task<ApiResponse<AccessToken>> GetAccessToken([Header("Authorization")] string authorizationHeader, [Header("Content-Type")] string contentType, [Body] string request);
    }
}