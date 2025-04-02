using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NServiceBus;

namespace Signify.PAD.Svc.Core.Events;

[ExcludeFromCodeCoverage]
public abstract class CdiEventBase : ICommand
{
    public Guid RequestId { get; set; }
    public long EvaluationId { get; set; }
    public DateTimeOffset DateTime { get; set; }
    public string UserName { get; set; }
    public string ApplicationId { get; set; }
    public List<DpsProduct> Products { get; set; }
}

[ExcludeFromCodeCoverage]
public class DpsProduct
{
    public long EvaluationId { get; set; }
    public string ProductCode { get; set; }
}