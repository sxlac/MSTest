using System.Text.Json;

namespace Signify.Dps.LabResultApi.CouchbasePoc.Models;

public class CreateLabResultRequest : CreateRequestBase
{
    /// <summary>
    /// Meta data from the application calling this API. Examples could be:
    ///
    /// - Filename (ex the name of the csv file, if result is from a file)
    /// - Version
    /// - etc
    /// </summary>
    public JsonElement MetaData { get; set; }

    /// <summary>
    /// Data received from the vendor. This would be the contents of the csv
    /// record (if from a csv file) serialized as JSON, or the raw JSON from
    /// the vendor (if sent to the Public Lab Result API).
    /// </summary>
    public JsonElement VendorData { get; set; }
}
