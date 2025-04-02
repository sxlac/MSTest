using NServiceBus;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Signify.Spirometry.Core.Events;

[ExcludeFromCodeCoverage]
public abstract class CdiEventBase : ICommand
{
    public Guid RequestId { get; set; }
    public long EvaluationId { get; set; }
    public DateTimeOffset DateTime { get; set; }
    public string ApplicationId { get; set; }
    public ICollection<DpsProduct> Products { get; set; } = new List<DpsProduct>();

    /// <summary>
    /// This is the DateTimeOffset at which the event is received by the PM.
    /// It is not part of the actual incoming CDIEvent
    /// </summary>
    public DateTimeOffset ReceivedBySpiroDateTime { get; set; }
}

[ExcludeFromCodeCoverage]
public class DpsProduct
{
    public long EvaluationId { get; set; }
    public string ProductCode { get; set; }
}
