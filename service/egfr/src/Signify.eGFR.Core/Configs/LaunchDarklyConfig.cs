namespace Signify.eGFR.Core.Configs;

public class LaunchDarklyConfig
{
    // License Keys
    public string SharedLicenseKey { get; set; }
    public string ProjectLicenseKey { get; set; }

    // Feature Flags
    public LaunchDarklyFlagConfig EnableEgfrLabResultsIngestion { get; set; }
    public LaunchDarklyFlagConfig EnableProviderPayCdi { get; set; }
    public LaunchDarklyFlagConfig EnableOrderCreation { get; set; }
    public LaunchDarklyFlagConfig EnableBillAccepted { get; set; }
    public LaunchDarklyFlagConfig EnableKedLabResultIngestion { get; set; } 
    
    public LaunchDarklyFlagConfig EnableDirectBilling { get; set; }
    public LaunchDarklyFlagConfig EnableInternalLabResultIngestion { get; set; }
}