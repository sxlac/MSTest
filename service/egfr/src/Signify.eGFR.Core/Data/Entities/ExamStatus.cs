using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.eGFR.Core.Data.Entities;

/// <summary>
/// Details about a status change event for a eGFR exam
/// </summary>
[ExcludeFromCodeCoverage]
public class ExamStatus
{
    /// <summary>
    /// Identifier of this exam status
    /// </summary>
    public int ExamStatusId { get; set; }
    /// <summary>
    /// Identifier of the eGFR exam this status corresponds to
    /// </summary>
    public int ExamId { get; set; }
    /// <summary>
    /// Identifier of the status code for this exam status
    /// </summary>
    public int ExamStatusCodeId { get; set; }
    /// <summary>
    /// The date and time when this status change event occurred
    /// </summary>
    public DateTimeOffset StatusDateTime { get; set; }
    /// <summary>
    /// The date and time when this notification was created within the eGFR process manager
    /// </summary>
    public DateTimeOffset CreatedDateTime { get; set; }

    /// <summary>
    /// The exam that this status update corresponds to
    /// </summary>
    public virtual Exam Exam { get; set; }
    /// <summary>
    /// The status code that this update corresponds to
    /// </summary>
    public virtual ExamStatusCode ExamStatusCode { get; set; }
}