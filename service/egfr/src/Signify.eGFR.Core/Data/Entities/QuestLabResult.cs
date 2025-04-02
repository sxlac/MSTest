using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Signify.eGFR.Core.Data.Entities;

/// <summary>
/// Details of a Quest eGFR LabResult that has been provided.
/// </summary>
[ExcludeFromCodeCoverage]
public class QuestLabResult
{
    /// <summary>
    /// Identifier of this eGFR LabResult
    /// </summary>
    [Key]
    public int LabResultId { get; set; }

    /// <summary>
    /// Identifier of the Member CenseoId
    /// </summary>
    public string CenseoId { get; set; }

    /// <summary>
    /// Identifier of the Vendor Lab Test Id 
    /// </summary>
    public long VendorLabTestId { get; set; }

    /// <summary>
    /// Identifier of the Vendor Lab Test Number
    /// </summary>
    public string VendorLabTestNumber { get; set; }

    /// <summary>
    /// Identifier of the Lab Result
    /// </summary>
    public int? eGFRResult { get; set; }

    /// <summary>
    /// Identifier of the Creatinine Result
    /// </summary>
    public decimal CreatinineResult { get; set; }

    /// <summary>
    /// Identifier of the Normality in text
    /// </summary>
    public string Normality { get; set; }

    /// <summary>
    /// Identifier of the Member Normality in Code
    /// </summary>
    public string NormalityCode { get; set; }

    /// <summary>
    /// Identifier of the Service MailDate
    /// </summary>
    public DateTimeOffset? MailDate { get; set; }

    /// <summary>
    /// Identifier of the Service CollectionDate
    /// </summary>
    public DateTimeOffset? CollectionDate { get; set; }

    /// <summary>
    /// Identifier of the Service AccessionedDate
    /// </summary>
    public DateTimeOffset? AccessionedDate { get; set; }

    /// <summary>
    /// When the eGFR process manager received this event
    /// </summary>
    public DateTimeOffset CreatedDateTime { get; set; }
}