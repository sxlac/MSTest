namespace IrisPoc.Models;

/// <summary>
/// Provider model taken pretty much copy-paste from DEE, allowing us to map DEE models to IRIS models in this POC
/// </summary>
public class ProviderModel
{
    public string? ProviderId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Npi { get; set; }
    public string? Email { get; set; }
}
