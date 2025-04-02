using System.Diagnostics.CodeAnalysis;

namespace Signify.PAD.Svc.Core.Configs;

[ExcludeFromCodeCoverage]
public class LaunchDarklyConfig
{
    // License Keys
    public string SharedLicenseKey { get; set; }
    public string ProjectLicenseKey { get; set; }

    // Feature Flags
    public LaunchDarklyFlagConfig EnableDlq { get; set; }
}