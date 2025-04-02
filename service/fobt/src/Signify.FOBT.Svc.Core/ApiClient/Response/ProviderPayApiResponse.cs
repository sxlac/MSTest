using System.Diagnostics.CodeAnalysis;

namespace Signify.FOBT.Svc.Core.ApiClient.Response;

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