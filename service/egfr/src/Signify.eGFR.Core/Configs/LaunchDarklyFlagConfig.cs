namespace Signify.eGFR.Core.Configs;

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