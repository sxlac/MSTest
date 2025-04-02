using System.Threading.Tasks;
using Refit;
using Signify.FOBT.Svc.Core.ApiClient.Requests;

namespace Signify.FOBT.Svc.Core.ApiClient;

public interface ILabsApi
{
    [Post("/orders")]
    [Headers("Authorization: Bearer")]
    Task<IApiResponse<int>> CreateOrder(CreateOrder order);
}