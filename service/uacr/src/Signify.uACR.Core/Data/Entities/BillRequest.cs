using System;
using System.Diagnostics.CodeAnalysis;
using Signify.uACR.Core.Constants;

namespace Signify.uACR.Core.Data.Entities;

[ExcludeFromCodeCoverage]
public class BillRequest
{
    public int BillRequestId { get; set; }
    public int ExamId { get; set; }
    public Guid BillId { get; set; }
    public DateTimeOffset? CreatedDateTime { get; set; }

    /// <summary>
    /// Whether the BillRequestAccepted event was received
    /// </summary>
    public bool? Accepted { get; set; }

    /// <summary>
    /// Date and time when the BillRequestSent table is updated with BillRequestAccepted event
    /// i.e. when the Accepted field is set to true
    /// </summary>
    public DateTimeOffset? AcceptedAt { get; set; }
    
    public virtual Exam Exam { get; set; }
    
    /// <summary>
    /// Product code of the type of billing
    /// </summary> 
    public string BillingProductCode { get; set; } = ProductCodes.uACR_RcmBilling;
}