using Refit;
using Signify.PAD.Svc.Core.ApiClient.Requests;
using System;
using System.Threading.Tasks;

namespace Signify.PAD.Svc.Core.ApiClient
{
    public interface IRcmApi
    {
        [Post("/Bills")]
        [Headers("Authorization: Bearer")]
        Task<IApiResponse<Guid?>> SendBillingRequest(CreateBillRequest createBillRequest);
    }
}
