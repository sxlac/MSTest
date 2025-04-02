using System;
using System.Threading.Tasks;
using Refit;
using Signify.CKD.Svc.Core.ApiClient.Requests;

namespace Signify.CKD.Svc.Core.ApiClient
{
    public interface IRcmApi
    {
        [Post("/Bills")]
        [Headers("Authorization: Bearer")]
        Task<IApiResponse<Guid?>> SendBillingRequest(RCMBilling rcmBillingRequest);
    }
}
