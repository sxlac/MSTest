using System.Threading.Tasks;
using Refit;
using Signify.CKD.Svc.Core.ApiClient.Requests;
using Signify.CKD.Svc.Core.ApiClient.Response;

namespace Signify.CKD.Svc.Core.ApiClient
{
    public interface IInventoryApi
	{
		[Post("/inventory")]
		[Headers("Authorization: Bearer")]
		Task<UpdateInventoryResponse> Inventory([Body] UpdateInventoryRequest request);
	}
}
