namespace Signify.CKD.Svc.Core.Configs;

public class LaunchDarklyConfig 
{
    
    // License Keys
    
    public string SharedLicenseKey { get; set; }
    public string ProjectLicenseKey { get; set; }
    
    // Feature Flags
    public LaunchDarklyFlagConfig EnableProviderPayCdi { get; set; }
}