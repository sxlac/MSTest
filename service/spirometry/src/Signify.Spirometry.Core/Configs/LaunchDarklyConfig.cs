using System.Diagnostics.CodeAnalysis;

namespace Signify.Spirometry.Core.Configs;

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
    
    public LaunchDarklyFlagConfig EnableBillAccepted { get; set; }
    public LaunchDarklyFlagConfig EnableDlq { get; set; }
}