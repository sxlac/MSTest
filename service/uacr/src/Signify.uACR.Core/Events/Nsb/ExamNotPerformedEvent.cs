using NotPerformedReason = Signify.uACR.Core.Models.NotPerformedReason;
using NServiceBus;
using Signify.uACR.Core.Data.Entities;
using System.Diagnostics.CodeAnalysis;
using System;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace UacrNsbEvents;

/// <summary>
/// Event corresponding to a uACR exam that was not performed by the provider (although the appointment
/// was schedule for one), including the reason why it was not performed
/// </summary>
[ExcludeFromCodeCoverage]
public class ExamNotPerformedEvent : IMessage
{
    /// <summary>
    /// Identifier of this event
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Entity information about the exam itself
    /// </summary>
    public Exam Exam { get; set; }

    /// <summary>
    /// Reason the provider supplied as to why the scheduled uACR exam was not performed during this evaluation
    /// </summary>
    public NotPerformedReason Reason { get; set; }
    
    /// <summary>
    /// Gets or sets the Reason Notes for not performed
    /// </summary>
    public string Notes { get; set; }
}