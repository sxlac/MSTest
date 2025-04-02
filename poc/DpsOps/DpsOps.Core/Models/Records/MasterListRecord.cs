namespace DpsOps.Core.Models.Records;

public class MasterListRecord
{
    public string Npi { get; init; }
    public string ShipToNumber { get; init; }

    public string AddressLine1 { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string ZipCode { get; set; }
}
