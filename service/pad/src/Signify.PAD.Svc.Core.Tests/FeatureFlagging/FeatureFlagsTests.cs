using FakeItEasy;
using LaunchDarkly.Sdk.Server;
using Signify.PAD.Svc.Core.Configs;
using Signify.PAD.Svc.Core.FeatureFlagging;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.FeatureFlagging;

public class FeatureFlagsTests
{
    [Fact]
    public void Constructor_WithSingleParameter_InitializesBaseClass()
    {
        // Arrange
        var ldConfig = A.Fake<LaunchDarklyConfig>();

        // Act
        var featureFlags = new FeatureFlags(ldConfig);

        // Assert
        Assert.NotNull(featureFlags);
        // Additional assertions can be added here to verify the state of the base class if accessible
    }

    [Fact]
    public void Constructor_WithMultipleParameters_InitializesBaseClass()
    {
        // Arrange
        var ldConfig = A.Fake<LaunchDarklyConfig>();
        var sharedClient = new LdClient("Test");
        var projectClient = new LdClient("Test");

        // Act
        var featureFlags = new FeatureFlags(ldConfig, sharedClient, projectClient);

        // Assert
        Assert.NotNull(featureFlags);
        // Additional assertions can be added here to verify the state of the base class if accessible
    }
}