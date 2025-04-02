using Refit;
using Signify.HBA1CPOC.Svc.Core.ApiClient.Response;
using System.Threading.Tasks;

namespace Signify.HBA1CPOC.Svc.Core.ApiClient;

public interface IOktaApi
{
    [Post("/oauth2/default/v1/token")]
    Task<IApiResponse<AccessToken>> GetAccessToken([Header("Authorization")] string authorizationHeader, [Header("Content-Type")] string contentType, [Body] string request);
}