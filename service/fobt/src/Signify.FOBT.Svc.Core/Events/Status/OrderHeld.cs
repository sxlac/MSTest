using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Signify.FOBT.Messages.Events.Status;

namespace Signify.FOBT.Svc.Core.Events.Status;

[ExcludeFromCodeCoverage]
public class OrderHeld : BaseStatusMessage
{
    public ICollection<string> HoldReasons { get; set; } = new List<string>();
    public DateTime HoldCreatedDate { get; set; }
    public OrderHeldContext Context { get; set; }
}

[ExcludeFromCodeCoverage]
public class OrderHeldContext
{
    public Guid? OrderId { get; set; }
    public string Barcode { get; set; }
    public DateTime? SampleReceivedDate { get; set; }
}