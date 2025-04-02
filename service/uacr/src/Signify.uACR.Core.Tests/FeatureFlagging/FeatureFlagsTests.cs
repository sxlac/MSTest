using LaunchDarkly.Sdk.Server;
using Signify.uACR.Core.Configs;
using Signify.uACR.Core.FeatureFlagging;
using Xunit;

namespace Signify.uACR.Core.Tests.FeatureFlagging;

public class FeatureFlagsTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FeatureFlags_Evaluates_ToCorrectState(bool flagValue)
    {
        //ARRANGE
        var mockLdConfig = new LaunchDarklyConfig
        {
            EnableOrderCreation = new LaunchDarklyFlagConfig(),
            EnableProviderPayCdi = new LaunchDarklyFlagConfig(),
            EnableBilling = new LaunchDarklyFlagConfig(),
            EnableLabResultIngestion = new LaunchDarklyFlagConfig(),
            EnableBillAccepted = new LaunchDarklyFlagConfig(),
            EnableDirectBilling = new LaunchDarklyFlagConfig()
        };
        var mockLdClientConfig = Configuration.Builder("FAKE_SDK_KEY")
            .Offline(true)
            .Build();
        var featureFlags = new FeatureFlags(mockLdConfig, new LdClient(mockLdClientConfig),
            new LdClient(mockLdClientConfig));
        
        mockLdConfig.EnableOrderCreation.FlagDefault = flagValue;
        mockLdConfig.EnableProviderPayCdi.FlagDefault = flagValue;
        mockLdConfig.EnableBilling.FlagDefault = flagValue;
        mockLdConfig.EnableLabResultIngestion.FlagDefault = flagValue;
        mockLdConfig.EnableBillAccepted.FlagDefault = flagValue;
        mockLdConfig.EnableDirectBilling.FlagDefault = flagValue;

        //ACT
        var enableOrderCreation = featureFlags.EnableOrderCreation;
        var enableProviderPayCdi = featureFlags.EnableProviderPayCdi;
        var enableBilling = featureFlags.EnableBilling;
        var enableLabResultIngestion = featureFlags.EnableLabResultIngestion;
        var enableBillAccepted = featureFlags.EnableBillAccepted;
        var enableDirectBilling = featureFlags.EnableDirectBilling;

        //ASSERT
        Assert.Equal(flagValue, enableOrderCreation);
        Assert.Equal(flagValue, enableProviderPayCdi);
        Assert.Equal(flagValue, enableBilling);
        Assert.Equal(flagValue, enableLabResultIngestion);
        Assert.Equal(flagValue, enableBillAccepted);
        Assert.Equal(flagValue, enableDirectBilling);
    }
}