namespace Signify.DEE.Svc.Core.Configs;

public class LaunchDarklyConfig
{
    // License Keys
    public string SharedLicenseKey { get; set; }
    public string ProjectLicenseKey { get; set; }

    // Feature Flags
    public LaunchDarklyFlagConfig EnableProviderPayCdi { get; set; }
    public LaunchDarklyFlagConfig EnableBillAccepted { get; set; }
    public LaunchDarklyFlagConfig EnableDlq { get; set; }
}