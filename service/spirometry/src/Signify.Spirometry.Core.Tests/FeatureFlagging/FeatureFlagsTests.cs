using LaunchDarkly.Sdk.Server;
using Signify.Spirometry.Core.Configs;
using Signify.Spirometry.Core.FeatureFlagging;
using Xunit;

namespace Signify.Spirometry.Core.Tests.FeatureFlagging;

public class FeatureFlagsTests
{
    [Fact]
    public void FeatureFlag_EnableBillAccepted_WhenDefaultedToTrue_EvaluatesToTrue()
    {
        //Arrange
        var mockLdConfig = new LaunchDarklyConfig { EnableBillAccepted = new LaunchDarklyFlagConfig() };
        var mockLdClientConfig = Configuration.Builder("FAKE_SDK_KEY")
            .Offline(true)
            .Build();
        var featureFlags = new FeatureFlags(mockLdConfig, new LdClient(mockLdClientConfig), new LdClient(mockLdClientConfig));
        
        mockLdConfig.EnableBillAccepted.FlagDefault = true;
        
        //Act
        var result = featureFlags.EnableBillAccepted;
        
        //Assert
        Assert.True(result);
    }

    [Fact]
    public void FeatureFlag_WhenDefaultedToFalse_EvaluatesToFalse()
    {
        //Arrange
        var mockLdConfig = new LaunchDarklyConfig { EnableBillAccepted = new LaunchDarklyFlagConfig() };
        var mockLdClientConfig = Configuration.Builder("FAKE_SDK_KEY")
            .Offline(true)
            .Build();
        var featureFlags = new FeatureFlags(mockLdConfig, new LdClient(mockLdClientConfig), new LdClient(mockLdClientConfig));

        mockLdConfig.EnableBillAccepted.FlagDefault = false;
        
        //Act
        var result = featureFlags.EnableBillAccepted;
        
        //Assert
        Assert.False(result);
    }

    [Fact]
    public void FeatureFlag_SharedFlagTypeUsed()
    {
        //Arrange
        var sharedFlagType = new LaunchDarklyFlagConfig{FlagType = LaunchDarklyFlagConfig.FeatureFlagType.Shared};
        var mockLdConfig = new LaunchDarklyConfig { EnableBillAccepted = sharedFlagType, SharedLicenseKey = "test"};
        var mockLdClientConfig = Configuration.Builder("FAKE_SDK_KEY")
            .Offline(true)
            .Build();
        var featureFlags = new FeatureFlags(mockLdConfig, new LdClient(mockLdClientConfig), new LdClient(mockLdClientConfig));

        mockLdConfig.EnableBillAccepted.FlagDefault = false;
        
        //Act
        var result = featureFlags.EnableBillAccepted;
        
        //Assert
        Assert.False(result);
    }

    [Fact]
    public void FeatureFlag_NoFlagTypeUsed()
    {
        //Arrange
        var sharedFlagType = new LaunchDarklyFlagConfig{
            FlagType = (LaunchDarklyFlagConfig.FeatureFlagType)999,
            FlagName = "test",
            FlagDefault = true};
        var mockLdConfig = new LaunchDarklyConfig { EnableBillAccepted = sharedFlagType, SharedLicenseKey = "test"};
        var mockLdClientConfig = Configuration.Builder("FAKE_SDK_KEY")
            .Offline(true)
            .Build();
        var featureFlags = new FeatureFlags(mockLdConfig, new LdClient(mockLdClientConfig), new LdClient(mockLdClientConfig));

        mockLdConfig.EnableBillAccepted.FlagDefault = false;
        
        //Act
        var result = featureFlags.EnableBillAccepted;
        
        //Assert
        Assert.False(result);
    }
}