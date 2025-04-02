using System.Collections.Generic;
using EgfrNsbEvents;

namespace Signify.eGFR.Core.Events.Status;

public class OrderRequestedStatusEvent : ExamStatusEvent
{
    public Dictionary<string, string> Context { get; set; }
    public string Vendor { get; set; }
}