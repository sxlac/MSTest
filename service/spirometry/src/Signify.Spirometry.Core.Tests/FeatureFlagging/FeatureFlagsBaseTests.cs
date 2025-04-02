using Signify.Spirometry.Core.Configs;
using Signify.Spirometry.Core.FeatureFlagging;
using Xunit;

namespace Signify.Spirometry.Core.Tests.FeatureFlagging;

public class FeatureFlagsBaseTests
{
    private class TestableFeatureFlagsBase : FeatureFlagsBase
    {
        public TestableFeatureFlagsBase(LaunchDarklyConfig ldConfig) : base(ldConfig)
        {
        }

        public LaunchDarklyConfig PublicLdConfig => LdConfig;
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
}