using System.Diagnostics.CodeAnalysis;

namespace Signify.Spirometry.Core.Configs;

[ExcludeFromCodeCoverage]
public class LaunchDarklyFlagConfig
{
    public string FlagName { get; set; }
    public bool FlagDefault { get; set; }
    public FeatureFlagType FlagType { get; set; }
    public enum FeatureFlagType {
        Project,
        Shared
    }
}