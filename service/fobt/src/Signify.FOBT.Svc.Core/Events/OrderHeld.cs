using NServiceBus;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Signify.FOBT.Svc.Core.Events
{
    [ExcludeFromCodeCoverage]
    public class OrderHeld : IMessage
    {
        public string ProductCode { get; set; }
        public string Barcode { get; set; }
        public ICollection<string> HoldReasons { get; set; } = new List<string>();
        public DateTime? LabReceivedDate { get; set; }
        public DateTime CreatedDateTime { get; set; }
    }
}
