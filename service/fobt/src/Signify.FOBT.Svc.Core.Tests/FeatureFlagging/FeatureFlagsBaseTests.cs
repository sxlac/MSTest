using Signify.FOBT.Svc.Core.Configs;
using Signify.FOBT.Svc.Core.FeatureFlagging;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.FeatureFlagging;

public class FeatureFlagsBaseTests
{
    private class TestableFeatureFlagsBase(LaunchDarklyConfig ldConfig) : FeatureFlagsBase(ldConfig)
    {
        public LaunchDarklyConfig PublicLdConfig => LdConfig;

        public bool EvaluateFlag(LaunchDarklyFlagConfig flag)
        {
            return Evaluate(flag);
        }
    }

    [Fact]
    public void FeatureFlag_Constructor_SetsLdConfigAsExpected()
    {
        // Arrange
        var ldConfig = new LaunchDarklyConfig { SharedLicenseKey = "shared-key", ProjectLicenseKey = "project-key" };

        // Act
        var featureFlags = new TestableFeatureFlagsBase(ldConfig);

        // Assert
        Assert.Equal(ldConfig, featureFlags.PublicLdConfig);
    }
    
    [Theory]
    [InlineData(LaunchDarklyFlagConfig.FeatureFlagType.Project)]
    [InlineData(LaunchDarklyFlagConfig.FeatureFlagType.Shared)]
    public void FeatureFlag_Evaluate_FlagType(object flagType)
    {
        // Arrange
        var ldConfig = new LaunchDarklyConfig { SharedLicenseKey = "shared-key", ProjectLicenseKey = "project-key" };
        var ldFlagConfig = new LaunchDarklyFlagConfig
        {
            FlagName = "testing",
            FlagDefault = false,
            FlagType = (LaunchDarklyFlagConfig.FeatureFlagType)flagType
        };

        // Act
        var featureFlags = new TestableFeatureFlagsBase(ldConfig);
        var flagTypeResult = featureFlags.EvaluateFlag(ldFlagConfig);

        // Assert
        Assert.False(flagTypeResult);
    }
}