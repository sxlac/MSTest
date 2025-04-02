using NServiceBus;
using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace EgfrEvents;

/// <summary>
/// Event received over Kafka from the PdfDeliveryCapPublisher service owned by C0reDump, running in the
/// signifycore-services k8s namespace.
/// </summary>
/// <remarks>
/// Class must be named PdfDeliveredToClient (and not PdfDeliveredToClientEvent) for it to be processed, as
/// this is the specific message type from the publishing service
/// </remarks>
public class PdfDeliveredToClient : IMessage
{
    private DateTimeOffset _deliveryDateTime;
    private DateTimeOffset _createdDateTime;

    /// <summary>
    /// Identifier of this event
    /// </summary>
    public Guid EventId { get; set; }
    /// <summary>
    /// Identifier of the evaluation associated with this PDF
    /// </summary>
    public long EvaluationId { get; set; }
    /// <summary>
    /// Product codes of the products included in this PDF
    /// </summary>
    public ICollection<string> ProductCodes { get; set; } = new List<string>();
    public long BatchId { get; set; }
    public string BatchName { get; set; }
    /// <summary>
    /// Timestamp the PDF was delivered to the client.
    /// DeliveryDateTime does not contain TimeZone in Kafka Event
    /// </summary>
    public DateTimeOffset DeliveryDateTime
    {
        get => _deliveryDateTime;
        set => _deliveryDateTime = new DateTimeOffset(value.DateTime, TimeSpan.Zero);
    }
    /// <summary>
    /// Timestamp this event was created
    /// CreatedDateTime does not contain TimeZone in Kafka Event
    /// </summary>
    public DateTimeOffset CreatedDateTime {
        get => _createdDateTime;
        set => _createdDateTime = new DateTimeOffset(value.DateTime, TimeSpan.Zero);
    }
}