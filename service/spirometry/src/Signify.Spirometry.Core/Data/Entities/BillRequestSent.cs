using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Signify.Spirometry.Core.Data.Entities;

/// <summary>
/// Details of a bill creation request that was successfully sent to RCM and acked
/// </summary>
// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global - Virtual properties are used by EF
[ExcludeFromCodeCoverage]
public class BillRequestSent
{
    /// <summary>
    /// Identifier of this entity
    /// </summary>
    [Key]
    public int BillRequestSentId { get; set; }
    /// <summary>
    /// FK Identifier to the associated <see cref="SpirometryExam"/> this billing request corresponds to
    /// </summary>
    public int SpirometryExamId { get; set; }
    /// <summary>
    /// Unique identifier of this RCM billing request, received in the ack from the RCM API
    /// </summary>
    public Guid BillId { get; set; }
    /// <summary>
    /// When the Spirometry process manager created this record
    /// </summary>
    public DateTime CreatedDateTime { get; set; }

    /// <summary>
    /// Whether the BillRequestAccepted event was received
    /// </summary>
    public bool? Accepted { get; set; }

    /// <summary>
    /// Date and time when the BillRequestSent table is updated with BillRequestAccepted event
    /// i.e. when the Accepted field is set to true
    /// </summary>
    public DateTimeOffset? AcceptedAt { get; set; }
        
    public virtual SpirometryExam SpirometryExam { get; set; }
}