namespace Signify.HBA1CPOC.System.Tests.Core.Models.Kafka;

public class CdiEvent
{
    public Guid RequestId { get; set; }
    public int EvaluationId { get; set; }
    public DateTimeOffset DateTime { get; set; }
    public string Username { get; set; }
    public string ApplicationId { get; set; }
    public string Reason { get; set; }
    public List<Product> Products { get; set; }
    public bool? PayProvider { get; set; }
}

public class Product
{
    public int EvaluationId { get; set; }
    public string ProductCode { get; set; }
}