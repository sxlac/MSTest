using System.Threading.Tasks;
using Refit;
using Signify.FOBT.Svc.Core.ApiClient.Requests;
using Signify.FOBT.Svc.Core.ApiClient.Response;

namespace Signify.FOBT.Svc.Core.ApiClient
{
	public interface IInventoryApi
	{
		[Post("/inventory")]
		[Headers("Authorization: Bearer")]
		Task<UpdateInventoryResponse> Inventory([Body] UpdateInventoryRequest request);
	}
}
