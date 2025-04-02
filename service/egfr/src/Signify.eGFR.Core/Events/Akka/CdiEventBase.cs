using System;
using System.Collections.Generic;
using NServiceBus;

namespace Signify.eGFR.Core.Events.Akka;

public abstract class CdiEventBase : ICommand
{
    public Guid RequestId { get; set; }
    public long EvaluationId { get; set; }
    public DateTimeOffset DateTime { get; set; }
    public string UserName { get; set; }
    public string ApplicationId { get; set; }
    public List<DpsProduct> Products { get; set; }
    public DateTimeOffset ReceivedByEgfrDateTime { get; set; }
}

public class DpsProduct
{
    public long EvaluationId { get; set; }
    public string ProductCode { get; set; }
}