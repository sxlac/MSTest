using Refit;
using System.Threading.Tasks;
using System;
using Signify.FOBT.Svc.Core.ApiClient.Requests;

namespace Signify.FOBT.Svc.Core.ApiClient;

public interface IRcmApi
{
    [Post("/Bills")]
    [Headers("Authorization: Bearer")]
    Task<IApiResponse<Guid?>> SendRcmRequestForBilling(RCMBilling rcmBillingRequest);
}