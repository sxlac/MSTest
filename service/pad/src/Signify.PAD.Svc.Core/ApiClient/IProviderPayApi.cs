using System.Threading.Tasks;
using Refit;
using Signify.PAD.Svc.Core.ApiClient.Requests;
using Signify.PAD.Svc.Core.ApiClient.Response;

namespace Signify.PAD.Svc.Core.ApiClient;

public interface IProviderPayApi
{
    /// <summary>
    /// Begins the process of creating a `Payment`.
    /// </summary>
    /// <param name="providerPayApiRequest"><see cref="ProviderPayApiRequest"/></param>
    /// <returns>Returns 202(Accepted) and a path used to poll for status of the resource's operation
    /// along with the PaymentId <see cref="ProviderPayApiResponse"/></returns>
    /// <remarks>https://developer.signifyhealth.com/catalog/default/api/providerpay/definition#/payments/createPayment</remarks>
    [Post("/payments")]
    [Headers("Authorization: Bearer")]
    Task<IApiResponse<ProviderPayApiResponse>> SendProviderPayRequest(ProviderPayApiRequest providerPayApiRequest);
}