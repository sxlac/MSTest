using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Models;
using System;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace SpiroNsbEvents;

/// <summary>
/// Event corresponding to a Spirometry exam that was performed by the provider
/// </summary>
[ExcludeFromCodeCoverage]
public class ExamPerformedEvent
{
    /// <summary>
    /// Identifier of this event
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Entity information about the exam itself
    /// </summary>
    public SpirometryExam Exam { get; set; }

    /// <summary>
    /// Raw, unvalidated results from the Evaluation API of the Spirometry exam that was performed by the provider
    /// </summary>
    public RawExamResult Result { get; set; }
}