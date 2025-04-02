using System;
using System.Collections.Generic;

namespace Signify.eGFR.Core.Events;

public class LabResultReceivedEvent
{
    /// <summary>
    /// Identifier of the LabResult
    /// </summary>
    public long LabResultId { get; set; }

    /// <summary>
    /// Identifier of the vendor
    /// </summary>
    public string VendorName { get; set; }
    
    /// <summary>
    /// List of products that were included in this evaluation
    /// </summary>
    public IEnumerable<string> ProductCodes { get; set; } = Array.Empty<string>();

    /// <summary>
    /// UTC timestamp results were received
    /// </summary>
    public DateTimeOffset ReceivedDateTime { get; set; }
}