namespace Signify.HBA1CPOC.System.Tests.Core.Models.Database;

public class HBA1CPOCStatusCode
{
    public int HBA1CPOCStatusCodeId { get; set; } 
   
    public string StatusCode { get; set; }

    public HBA1CPOCStatusCode(int code, string name)
    {
        HBA1CPOCStatusCodeId = code;
        StatusCode = name;
    }
}