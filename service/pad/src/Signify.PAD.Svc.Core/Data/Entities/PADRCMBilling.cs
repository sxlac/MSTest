using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.PAD.Svc.Core.Data.Entities;

[ExcludeFromCodeCoverage]
public class PADRCMBilling
{
    public PADRCMBilling()
    {
    }

    /// <summary>
    /// Identifier of this entity
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Unique identifier of this RCM billing request, received in the ack from the RCM API
    /// </summary>
    public string BillId { get; set; }

    //Foreign key
    public virtual PAD PAD { get; set; }

    /// <summary>
    /// FK Identifier to the associated <see cref="PAD"/> this billing request corresponds to
    /// </summary>
    public int PADId { get; set; }

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

    public override string ToString() =>
        $"{nameof(Id)}: {Id}, {nameof(BillId)}: {BillId}, {nameof(PADId)}: {PADId}, {nameof(CreatedDateTime)}: {CreatedDateTime}, {nameof(Accepted)}: {Accepted}, {nameof(AcceptedAt)}: {AcceptedAt}";
}