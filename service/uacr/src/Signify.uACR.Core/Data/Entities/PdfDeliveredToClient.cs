using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.uACR.Core.Data.Entities;

[ExcludeFromCodeCoverage]
public class PdfDeliveredToClient
{
    public int PdfDeliveredToClientId { get; set; }
    public Guid EventId { get; set; }
    public long EvaluationId { get; set; }
    public long BatchId { get; set; }
    public string BatchName { get; set; }
    public DateTimeOffset DeliveryDateTime { get; set; }
    public DateTimeOffset CreatedDateTime { get; set; }
}