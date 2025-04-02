using System.Diagnostics.CodeAnalysis;

namespace Signify.uACR.Core.ApiClients.ProviderPayAPi.Responses;

/// <summary>
/// Response from the ProviderPay API when initiating a payment
/// </summary>
[ExcludeFromCodeCoverage]
public class ProviderPayApiResponse
{
    /// <summary>
    /// Unique guid of the Payment 
    /// </summary>
    public string PaymentId { get; set; }
}