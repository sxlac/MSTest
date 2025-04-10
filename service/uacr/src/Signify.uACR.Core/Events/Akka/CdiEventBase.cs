using NServiceBus;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System;

namespace Signify.uACR.Core.Events.Akka;

[ExcludeFromCodeCoverage]
public abstract class CdiEventBase : ICommand
{
    public Guid RequestId { get; set; }
    public long EvaluationId { get; set; }
    public DateTimeOffset DateTime { get; set; }
    public string UserName { get; set; }
    public string ApplicationId { get; set; }
    public List<DpsProduct> Products { get; set; }
    public DateTimeOffset ReceivedByUacrDateTime { get; set; }
}

[ExcludeFromCodeCoverage]
public class DpsProduct
{
    public long EvaluationId { get; set; }
    public string ProductCode { get; set; }
}