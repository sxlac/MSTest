using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Signify.uACR.Core.ApiClients.ProviderPayAPi.Requests;

/// <summary>
/// Request object for ProviderPay API to create a payment
/// </summary> 
/// <remarks>https://developer.signifyhealth.com/catalog/default/api/providerpay/definition#/payments/createPayment</remarks>
[ExcludeFromCodeCoverage]
public class ProviderPayApiRequest
{
    /// <summary>
    /// Identifier of the Provider
    /// </summary>
    public long ProviderId { get; set; }

    /// <summary>
    /// Product Code of the exam
    /// </summary>
    public string ProviderProductCode { get; set; }

    /// <summary>
    /// CenseoId
    /// </summary>
    public string PersonId { get; set; }

    /// <summary>
    /// Date of the IHE
    /// </summary>
    public string DateOfService { get; set; }

    /// <summary>
    /// Identifier of the Client
    /// </summary>
    public int ClientId { get; set; }

    /// <summary>
    /// Additional context info that can be used to send details like EvaluationId, AppointmentId
    /// </summary>
    public Dictionary<string, string> AdditionalDetails { get; set; }
}