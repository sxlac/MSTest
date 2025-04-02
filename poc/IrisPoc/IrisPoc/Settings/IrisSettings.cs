namespace IrisPoc.Settings;

/// <summary>
/// IRIS-specific settings
/// </summary>
public class IrisSettings
{
    /// <summary>
    /// Identifier of our site. Signify has a single site in each environment, and the identifier just so
    /// happens to have the same value in QA vs production. This field is required when submitting orders.
    /// </summary>
    public string? SiteLocalId { get; set; }

    /// <summary>
    /// Identifies the submission source organization; ie unique <see cref="Guid"/> for Signify Health
    /// </summary>
    public Guid ClientGuid { get; set; }
}
