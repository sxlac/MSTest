using System.Threading.Tasks;
using Refit;
using Signify.DEE.Svc.Core.Infrastructure;

namespace Signify.DEE.Svc.Core.ApiClient;

public interface IOAuthApi
{
    [Post("/connect/token")]
    Task<AccessToken> GetToken([Header("Authorization")] string authorization, [Header("Content-Type")] string contentType, [Body] string body);

    [Post("/oauth/token")]
    Task<AccessToken> GetIrisTokenV1([Header("Accept")] string accept, [Header("Content-Type")] string contentType, [Body] string body);

    [Post("/oauth2/v2.0/token")]
    Task<AccessToken> GetIrisToken([Header("Accept")] string accept, [Header("Content-Type")] string contentType, [Body] string body);
}