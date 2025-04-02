using Refit;
using Signify.HBA1CPOC.Svc.Core.ApiClient.Requests;
using Signify.HBA1CPOC.Svc.Core.ApiClient.Response;
using System.Threading.Tasks;

namespace Signify.HBA1CPOC.Svc.Core.ApiClient;

public interface IInventoryApi
{
	[Post("/inventory")]
	[Headers("Authorization: Bearer")]
	Task<UpdateInventoryResponse> Inventory([Body] UpdateInventoryRequest request);
}