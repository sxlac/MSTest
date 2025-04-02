using System;
using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace Signify.FOBT.Svc.Core.Data.Entities;

// ReSharper disable once InconsistentNaming
[ExcludeFromCodeCoverage]
public partial class FOBTBilling
{
    public FOBTBilling()
    {
    }

    /// <summary>
    /// Identifier of this entity
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// FK Identifier to the associated <see cref="FOBT"/> this billing request corresponds to
    /// </summary>
    public int FOBTId { get; set; }

    /// <summary>
    /// Unique identifier of this RCM billing request, received in the ack from the RCM API
    /// </summary>
    public string BillId { get; set; }

    /// <summary>
    /// Product code of the type of billing
    /// </summary> 
    public string BillingProductCode { get; set; }

    /// <summary>
    /// When the process manager created this record
    /// </summary> 
    public DateTimeOffset CreatedDateTime { get; set; }

    /// <summary>
    /// Whether the BillRequestAccepted event was received
    /// </summary>
    public bool? Accepted { get; set; }

    /// <summary>
    /// Date and time when the BillRequestSent table is updated with BillRequestAccepted event
    /// i.e. when the Accepted field is set to true
    /// </summary>
    public DateTimeOffset? AcceptedAt { get; set; }
}