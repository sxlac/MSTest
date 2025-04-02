using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Signify.FOBT.Svc.Core.Data.Entities;

[ExcludeFromCodeCoverage]
public sealed class LabResults
{
    [Key]
    public int LabResultId { get; set; }
    public Guid OrderCorrelationId { get; set; }
    public string Barcode { get; set; }
    public string LabResult { get; set; }
    public string ProductCode { get; set; }
    public string AbnormalIndicator { get; set; }
    public string Exception { get; set; }
    public DateTime? CollectionDate { get; set; }
    public DateTime? ServiceDate { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public DateTime? CreatedDateTime { get; set; }
    public int FOBTId { get; set; }
}