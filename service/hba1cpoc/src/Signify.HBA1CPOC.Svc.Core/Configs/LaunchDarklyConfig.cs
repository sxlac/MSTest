using System.Diagnostics.CodeAnalysis;

namespace Signify.HBA1CPOC.Svc.Core.Configs;

[ExcludeFromCodeCoverage]
public class LaunchDarklyConfig 
{
    // License Keys
    public string SharedLicenseKey { get; set; }
    public string ProjectLicenseKey { get; set; }
    
    // Feature Flags
}