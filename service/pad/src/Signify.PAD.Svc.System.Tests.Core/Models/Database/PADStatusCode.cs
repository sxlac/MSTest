namespace Signify.PAD.Svc.System.Tests.Core.Models.Database;

public class PADStatusCode
{
    public int PADStatusCodeId { get; set; }
    public string StatusCode { get; set; }

    public PADStatusCode(int id, string statusCode)
    {
        PADStatusCodeId = id;
        StatusCode = statusCode;
    }
}