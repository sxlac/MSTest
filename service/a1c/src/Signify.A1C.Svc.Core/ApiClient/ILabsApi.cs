using System.Threading.Tasks;
using Refit;
using Signify.A1C.Svc.Core.ApiClient.Requests;

namespace Signify.A1C.Svc.Core.ApiClient
{
    public interface ILabsApi
    {
        [Post("/orders")]
        [Headers("Authorization: Bearer")]
        Task<ApiResponse<int>> CreateOrder(CreateOrder order);
    }
}
