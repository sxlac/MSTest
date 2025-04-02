using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Signify.Spirometry.Core.Data.Entities;

/// <summary>
/// Details of a notification event notifying the Spirometry process manager that a PDF containing
/// details of a spirometry exam have been delivered to the client.
/// </summary>
[ExcludeFromCodeCoverage]
public class PdfDeliveredToClient
{
    /// <summary>
    /// PK identifier of this entity
    /// </summary>
    [Key]
    public int PdfDeliveredToClientId { get; set; }
    /// <summary>
    /// Unique identifier of the event
    /// </summary>
    public Guid EventId { get; set; }
    /// <summary>
    /// FK identifier for the evaluation this event corresponds to
    /// </summary>
    public long EvaluationId { get; set; }
    public long BatchId { get; set; }
    public string BatchName { get; set; }
    /// <summary>
    /// Timestamp this PDF was delivered to the client
    /// </summary>
    public DateTime DeliveryDateTime { get; set; }
    /// <summary>
    /// Timestamp this PDF was created within the other Signify PDF service
    /// </summary>
    public DateTime CreatedDateTime { get; set; }
}