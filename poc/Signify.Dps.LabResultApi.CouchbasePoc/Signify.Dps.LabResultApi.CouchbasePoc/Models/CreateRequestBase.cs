namespace Signify.Dps.LabResultApi.CouchbasePoc.Models;

public abstract class CreateRequestBase
{
    /// <summary>
    /// Identifier of the order this result corresponds to
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// Name of the vendor this result corresponds to
    /// </summary>
    public string VendorName { get; set; }

    /// <summary>
    /// Collection of one or more product codes associated with the request
    /// </summary>
    public ISet<string> ProductCodes { get; set; }
}
