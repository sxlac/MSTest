using Refit;
using Signify.DEE.Svc.Core.Messages.Models;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.ApiClient;

public interface IProviderApi
{
    [Get("/Providers/{providerId}")]
    [Headers("Authorization: Bearer")]
    Task<IApiResponse<ProviderModel>> GetProviderById(int providerId);
}