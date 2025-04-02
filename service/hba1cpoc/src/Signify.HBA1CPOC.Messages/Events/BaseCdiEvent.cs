using System;
using System.Collections.Generic;
using NServiceBus;

namespace Signify.HBA1CPOC.Messages.Events;

public abstract class BaseCdiEvent : ICommand
{
    public Guid RequestId { get; set; }
    public long EvaluationId { get; set; }
    public DateTimeOffset DateTime { get; set; }
    public string UserName { get; set; }
    public string ApplicationId { get; set; }
    public List<DpsProduct> Products { get; set; }
}

public class DpsProduct
{
    public long EvaluationId { get; set; }
    public string ProductCode { get; set; }
    
    public DpsProduct(long evaluationId, string productCode)
    {
        EvaluationId = evaluationId;
        ProductCode = productCode;
    }

    public override string ToString()
    {
        return $"{nameof(ProductCode)}: {ProductCode}";
    }
}