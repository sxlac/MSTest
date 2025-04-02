using System;
using Signify.eGFR.Core.ApiClients.RcmApi.Requests;

namespace Signify.eGFR.Core.ApiClients.RcmApi.Responses;

/// <summary>
/// Response type for performing a POST to the Bills endpoint on the <see cref="IRcmApi"/>.
///
/// See: https://chgit.censeohealth.com/projects/RCM/repos/signify.rcm.billing/browse/src/Signify.RCM.Billing.API.Common/Models/CreateBillResponse.cs
/// </summary>
public class CreateBillResponse
{
    public Guid RcmTransactionId { get; set; }

    /// <summary>
    /// Timestamp the <see cref="CreateBillRequest"/> was received by the RCM API
    /// </summary>
    public DateTimeOffset ReceivedDateTime { get; set; }
}