using System;
using System.ComponentModel.DataAnnotations;

namespace Signify.eGFR.Core.Data.Entities;

/// <summary>
/// Details of a notification event notifying the eGFR process manager that a PDF containing
/// details of a eGFR exam have been delivered to the client.
/// </summary>
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
    public DateTimeOffset DeliveryDateTime { get; set; }
    /// <summary>
    /// Timestamp this PDF was created within the other Signify PDF service
    /// </summary>
    public DateTimeOffset CreatedDateTime { get; set; }
}