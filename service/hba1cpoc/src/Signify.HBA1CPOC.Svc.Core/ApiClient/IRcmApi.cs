using Refit;
using Signify.HBA1CPOC.Svc.Core.ApiClient.Requests;
using System;
using System.Threading.Tasks;

namespace Signify.HBA1CPOC.Svc.Core.ApiClient;

public interface IRcmApi
{
    [Post("/Bills")]
    [Headers("Authorization: Bearer")]
    Task<IApiResponse<Guid?>> SendBillingRequest(CreateBillRequest createBillRequestRequest);
}