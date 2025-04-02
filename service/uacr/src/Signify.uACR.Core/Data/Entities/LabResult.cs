using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Signify.uACR.Core.Data.Entities;

[ExcludeFromCodeCoverage]
public class LabResult
{
    /// <summary>
    /// Identifier of this uACR LabResult
    /// </summary>
    [Key]
    public int LabResultId { get; set; }
    /// <summary>
    /// Identifier of this evaluation
    /// </summary>
    public long EvaluationId { get; set; }
    
    /// <summary>
    /// Identifier of the Service MailDate
    /// </summary>
    public DateTimeOffset ReceivedDate { get; set; }
    
    /// <summary>
    /// Identifier of the Lab Result
    /// </summary>
    public decimal? UacrResult { get; set; }
    
    /// <summary>
    /// Used to determine the normality i.e. Red High/Low = Abnormal; Grey=Inconclusive; All other colours = Normal. Used for the "Determination" field for Lab results A/N/U
    /// </summary>
    public string ResultColor { get; set; }
    
    /// <summary>
    /// Identifier of the Normality in text
    /// </summary>
    public string Normality { get; set; }

    /// <summary>
    /// Identifier of the Member Normality in Code
    /// </summary>
    public string NormalityCode { get; set; }
    
    /// <summary>
    /// This is populated for the result description 
    /// </summary>
    public string ResultDescription { get; set; }
    
    /// <summary>
    /// When the uACR process manager received this event
    /// </summary>
    public DateTimeOffset CreatedDateTime { get; set; }
}