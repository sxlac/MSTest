using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Models;
using System;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace SpiroNsbEvents;

/// <summary>
/// Event corresponding to a Spirometry exam that was not performed by the provider (although the appointment
/// was schedule for one), including the reason why it was not performed
/// </summary>
[ExcludeFromCodeCoverage]
public class ExamNotPerformedEvent
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
    /// Details the provider supplied as to why the scheduled Spirometry exam was not performed during this evaluation
    /// </summary>
    public NotPerformedInfo Info { get; set; }
}