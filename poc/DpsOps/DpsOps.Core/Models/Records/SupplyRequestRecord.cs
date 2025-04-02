namespace DpsOps.Core.Models.Records;

public class SupplyRequestRecord
{
    public string Npi { get; init; }

    public string AddressLine1 { get; set; }
    public string AddressLine2 { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string ZipCode { get; set; }
}
