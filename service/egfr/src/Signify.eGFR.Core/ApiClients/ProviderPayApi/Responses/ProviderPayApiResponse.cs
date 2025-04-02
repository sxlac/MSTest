namespace Signify.eGFR.Core.ApiClients.ProviderPayApi.Responses;

/// <summary>
/// Response from the ProviderPay API when initiating a payment
/// </summary>
public class ProviderPayApiResponse
{
    /// <summary>
    /// Unique guid of the Payment 
    /// </summary>
    public string PaymentId { get; set; }
}