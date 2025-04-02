namespace Signify.Dps.LabResultApi.CouchbasePoc;

public static class CouchbaseConstants
{
    // A Couchbase cluster has zero or more buckets
    // Buckets have one or more scopes
    // Scopes have zero or more collections

    // Although not the same, you could think of it as:
    // Collections as tables, Scopes as schemas, and Buckets as databases

    /// <summary>
    /// Name of the Couchbase bucket for this application
    /// </summary>
    public const string Bucket = "dps-rms";

    /// <summary>
    /// Name of the Couchbase scope for this application
    /// </summary>
    public const string Scope = "ilrapi-scope";

    /// <summary>
    /// Name of the Couchbase collection where lab results are stored
    /// </summary>
    public const string LabResultCollection = "lab-result";

    /// <summary>
    /// Name of the Couchbase collection where pdf documents are stored
    /// </summary>
    public const string LabPdfCollection = "lab-pdf";
}