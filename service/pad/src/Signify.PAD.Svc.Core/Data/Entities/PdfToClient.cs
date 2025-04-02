using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Signify.PAD.Svc.Core.Data.Entities;

[ExcludeFromCodeCoverage]
public class PDFToClient
{
    [Key]
    public int PDFDeliverId { get; set; }
    public string EventId { get; set; }
    public long EvaluationId { get; set; }
    public DateTime DeliveryDateTime { get; set; }
    public DateTime DeliveryCreatedDateTime { get; set; }
    public long BatchId { get; set; }
    public string BatchName { get; set; }
    public int PADId { get; set; }
    public DateTime CreatedDateTime { get; set; }
}