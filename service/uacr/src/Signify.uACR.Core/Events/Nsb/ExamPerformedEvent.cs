using NServiceBus;
using Signify.uACR.Core.Data.Entities;
using Signify.uACR.Core.Models;
using System.Diagnostics.CodeAnalysis;
using System;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace UacrNsbEvents;

/// <summary>
/// Event corresponding to a UACR exam that was performed by the provider
/// </summary>
[ExcludeFromCodeCoverage]
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
    /// Raw, unvalidated results from the Evaluation API of the UACR exam that was performed by the provider
    /// </summary>
    public RawExamResult Result { get; set; }
}