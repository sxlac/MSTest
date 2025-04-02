using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UacrNsbEvents;

namespace Signify.uACR.Core.Events.Status;

[ExcludeFromCodeCoverage]
public class OrderRequestedStatusEvent : ExamStatusEvent
{
    public Dictionary<string, string> Context { get; set; }
    public string Vendor { get; set; }    
}