using Refit;
using Signify.DEE.Svc.Core.Infrastructure;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.ApiClient;

public interface IOktaApi
{
    [Post("/oauth2/default/v1/token")]
    Task<IApiResponse<AccessToken>> GetAccessToken([Header("Authorization")] string authorizationHeader, [Header("Content-Type")] string contentType, [Body] string request);
}