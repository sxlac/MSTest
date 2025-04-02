using NServiceBus;
using System;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace FobtNsbEvents;

[ExcludeFromCodeCoverage]
public class HomeAccessResultsReceived : IMessage
{
    public Guid EventId { get; set; }
    public DateTime CreatedDateTime { get; set; }
    public Guid OrderCorrelationId { get; set; }
    public string Barcode { get; set; }
    public string LabTestType { get; set; }
    public string LabResults { get; set; }
    public string AbnormalIndicator { get; set; }
    public string Exception { get; set; }
    public DateTime? CollectionDate { get; set; }
    public DateTime? ServiceDate { get; set; }
    public DateTime? ReleaseDate { get; set; }
}