using NServiceBus;
using Signify.eGFR.Core.Data.Entities;
using System;
using NotPerformedReason = Signify.eGFR.Core.Models.NotPerformedReason;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace EgfrNsbEvents;

/// <summary>
/// Event corresponding to a eGFR exam that was not performed by the provider (although the appointment
/// was schedule for one), including the reason why it was not performed
/// </summary>
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
    /// Reason the provider supplied as to why the scheduled eGFR exam was not performed during this evaluation
    /// </summary>
    public NotPerformedReason Reason { get; set; }
    
    /// <summary>
    /// Gets or sets the Reason Notes for not performed
    /// </summary>
    public string Notes { get; set; }
}