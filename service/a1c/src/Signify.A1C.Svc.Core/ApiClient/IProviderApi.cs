using System.Threading.Tasks;
using Refit;
using Signify.A1C.Svc.Core.ApiClient.Response;

namespace Signify.A1C.Svc.Core.ApiClient
{
    public interface IProviderApi
	{
		[Get("/Providers/{providerId}")]
		[Headers("Authorization: Bearer")]
		Task<ApiResponse<ProviderInfoRs>> GetProviderById(int providerId);
	}
}
