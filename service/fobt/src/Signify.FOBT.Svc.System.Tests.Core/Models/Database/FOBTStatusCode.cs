namespace Signify.FOBT.Svc.System.Tests.Core.Models.Database;

public class FOBTStatusCode
{
    public int FOBTStatusCodeId { get; set; }
    public string StatusCode { get; set; }

    public FOBTStatusCode(int id, string statusCode)
    {
        FOBTStatusCodeId = id;
        StatusCode = statusCode;
    }
}