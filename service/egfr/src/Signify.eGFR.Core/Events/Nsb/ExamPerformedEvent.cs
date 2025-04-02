using NServiceBus;
using Signify.eGFR.Core.Data.Entities;
using Signify.eGFR.Core.Models;
using System;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace EgfrNsbEvents;

/// <summary>
/// Event corresponding to a eGFR exam that was performed by the provider
/// </summary>
public class ExamPerformedEvent : IMessage
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
    /// Raw, unvalidated results from the Evaluation API of the eGFR exam that was performed by the provider
    /// </summary>
    public RawExamResult Result { get; set; }
}