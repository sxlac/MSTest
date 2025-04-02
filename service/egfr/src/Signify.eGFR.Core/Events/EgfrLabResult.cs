using System;
using NServiceBus;

namespace Signify.eGFR.Core.Events;

public class EgfrLabResult : IMessage
{
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
    /// Date and Time when the event was received by eGFR PM
    /// </summary>
    public DateTimeOffset ReceivedByEgfrDateTime { get; set; }
}