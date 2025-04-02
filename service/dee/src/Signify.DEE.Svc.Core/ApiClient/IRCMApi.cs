using Refit;
using Signify.DEE.Svc.Core.ApiClient.Requests;
using System.Threading.Tasks;
using System;

namespace Signify.DEE.Svc.Core.ApiClient;

public interface IRCMApi
{
    [Post("/Bills")]
    [Headers("Authorization: Bearer")]
    Task<IApiResponse<Guid?>> SendRCMRequestForBilling(RCMBilling rcmBillingRequest);
}