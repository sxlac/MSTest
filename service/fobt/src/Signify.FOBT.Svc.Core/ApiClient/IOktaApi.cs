using System.Threading.Tasks;
using Refit;
using Signify.FOBT.Svc.Core.ApiClient.Response;

namespace Signify.FOBT.Svc.Core.ApiClient;

public interface IOktaApi
{
    [Post("/oauth2/default/v1/token")]
    Task<IApiResponse<AccessToken>> GetAccessToken([Header("Authorization")] string authorizationHeader, [Header("Content-Type")] string contentType, [Body] string request);
}