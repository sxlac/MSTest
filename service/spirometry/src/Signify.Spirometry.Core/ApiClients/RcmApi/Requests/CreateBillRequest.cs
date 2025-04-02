using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Signify.Spirometry.Core.ApiClients.RcmApi.Requests;

/// <summary>
/// Request to create a new bill for generating revenue.
///
/// See https://chgit.censeohealth.com/projects/RCM/repos/signify.rcm.billing/browse/src/Signify.RCM.Billing.API.Common/Models/CreateBillRequest.cs
/// </summary>
[ExcludeFromCodeCoverage]
public class CreateBillRequest
{
    /// <summary>
    /// ie ClientId, this is the ClientId as it exists in Virtus
    /// </summary>
    /// <remarks>
    /// Although nullable, required by RCM to create a bill
    /// </remarks>
    public int? SharedClientId { get; set; }
    /// <summary>
    /// Identifier of the member plan
    /// </summary>
    /// <remarks>
    /// Although nullable, required by RCM to create a bill
    /// </remarks>
    public int? MemberPlanId { get; set; }
    /// <summary>
    /// Date the product was delivered or service rendered
    /// </summary>
    /// <remarks>
    /// Only the Day portion is required, and must not be affected by UTC time gaps.
    ///
    /// Although nullable, required by RCM to create a bill
    /// </remarks>
    public DateTime? DateOfService { get; set; }
    /// <summary>
    /// The geographic state in which the product was delivered or service rendered
    /// </summary>
    /// <remarks>
    /// Although nullable, required by RCM to create a bill
    /// </remarks>
    public string UsStateOfService { get; set; }
    /// <summary>
    /// Identifier of the provider
    /// </summary>
    /// <remarks>
    /// Although nullable, required by RCM to create a bill
    /// </remarks>
    public long? ProviderId { get; set; }
    /// <summary>
    /// The code representing the product as it is configured in the RCM system
    /// </summary>
    /// <remarks>
    /// Although nullable, required by RCM to create a bill
    /// </remarks>
    public string RcmProductCode { get; } = Constants.ProductCodes.Spirometry;
    /// <summary>
    /// Unique identifier of the application. This will be supplied by an RCM team member.
    /// </summary>
    /// <remarks>
    /// Cannot be null or contain whitespace, or RCM will not create a bill
    /// </remarks>
    public string ApplicationId { get; set; }
    /// <summary>
    /// The date the product or service became billable. This is dependent on the Billable Event for each product.
    /// </summary>
    /// <remarks>
    /// Although nullable, required by RCM to create a bill
    /// </remarks>
    public DateTime? BillableDate { get; set; }
    /// <remarks>
    /// Although nullable, required by RCM to create a bill
    /// </remarks>
    public string CorrelationId { get; set; }
    /// <summary>
    /// A set of string key/value pairs that is specific to the product
    /// </summary>
    public Dictionary<string, string> AdditionalDetails { get; set; } = new Dictionary<string, string>();
    //public string Username { get; set; } // Deprecated
}