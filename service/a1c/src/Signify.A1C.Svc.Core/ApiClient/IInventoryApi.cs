using System.Threading.Tasks;
using Refit;
using Signify.A1C.Svc.Core.ApiClient.Requests;
using Signify.A1C.Svc.Core.ApiClient.Response;

namespace Signify.A1C.Svc.Core.ApiClient
{
	public interface IInventoryApi
	{
		[Post("/inventory")]
		[Headers("Authorization: Bearer")]
		Task<UpdateInventoryResponse> Inventory([Body] UpdateInventoryRequest request);
	}
}
