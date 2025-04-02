using System.Diagnostics.CodeAnalysis;

namespace Signify.uACR.Core.Configs;

[ExcludeFromCodeCoverage]
public class LaunchDarklyConfig
{
    /// <summary>
    /// Configuration section key for this config
    /// </summary>
    public const string Key = "LaunchDarkly";
    
    // License Keys
    public string SharedLicenseKey { get; set; }
    public string ProjectLicenseKey { get; set; }
    
    // Feature Flags
    public LaunchDarklyFlagConfig EnableOrderCreation { get; set; }
    
    public LaunchDarklyFlagConfig EnableProviderPayCdi { get; set; }

    public LaunchDarklyFlagConfig EnableBilling { get; set; }
    
    public LaunchDarklyFlagConfig EnableLabResultIngestion { get; set; }
    
    public LaunchDarklyFlagConfig EnableBillAccepted { get; set; }
    
    public LaunchDarklyFlagConfig EnableDirectBilling { get; set; }

    public LaunchDarklyFlagConfig EnableInternalLabResultIngestion { get; set; }
    
    public LaunchDarklyFlagConfig EnableDlq { get; set; }
}