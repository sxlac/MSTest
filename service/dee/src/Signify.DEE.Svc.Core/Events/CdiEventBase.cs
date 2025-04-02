using System;
using System.Collections.Generic;
using NServiceBus;

namespace Signify.DEE.Svc.Core.Events;

public abstract class CdiEventBase : ICommand
{
    public Guid RequestId { get; set; }
    public long EvaluationId { get; set; }
    public DateTimeOffset DateTime { get; set; }
    public string UserName { get; set; }
    public string ApplicationId { get; set; }
    public List<DpsProduct> Products { get; set; }

    /// <summary>
    /// This is the DateTimeOffset at which the event is received by the PM.
    /// It is not part of the actual incoming CDIEvent
    /// </summary>
    public DateTimeOffset ReceivedByDeeDateTime { get; set; }
}

public class DpsProduct
{
    public long EvaluationId { get; set; }
    public string ProductCode { get; set; }
}