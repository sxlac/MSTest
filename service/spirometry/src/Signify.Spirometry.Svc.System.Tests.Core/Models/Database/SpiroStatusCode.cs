namespace Signify.Spirometry.Svc.System.Tests.Core.Models.Database;

public class SpiroStatusCode
{
    public int SpiroStatusCodeId { get; set; }
    public string StatusCode { get; set; }

    public SpiroStatusCode(int id, string statusCode)
    {
        SpiroStatusCodeId = id;
        StatusCode = statusCode;
    }
}