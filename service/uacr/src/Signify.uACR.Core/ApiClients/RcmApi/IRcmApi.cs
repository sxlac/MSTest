using System;
using System.Threading.Tasks;
using Refit;
using Signify.uACR.Core.ApiClients.RcmApi.Requests;

namespace Signify.uACR.Core.ApiClients.RcmApi;

/// <summary>
/// Interface to make requests to the Signify RCM (Revenue Cycle Management) API
///
/// See https://dev.azure.com/signifyhealth/HCC/_git/rcm?path=/service/rcm.billing/src/Signify.RCM.Billing.Api/Controllers/V2/BillsController.cs
/// </summary>
public interface IRcmApi
{
    /// <summary>
    /// Send a request to generate a new bill for revenue
    /// </summary>
    /// <param name="createBillRequest"></param>
    /// <returns>Response with content being the Billing Id</returns>
    [Post("/bills")]
    [Headers("Authorization: Bearer")]
    Task<IApiResponse<Guid?>> SendBillingRequest(CreateBillRequest createBillRequest);
}