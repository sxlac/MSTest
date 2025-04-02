using LaunchDarkly.Sdk.Server;
using Signify.eGFR.Core.Configs;
using Signify.eGFR.Core.FeatureFlagging;
using Xunit;

namespace Signify.eGFR.Core.Tests.FeatureFlagging;

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
            EnableEgfrLabResultsIngestion = new LaunchDarklyFlagConfig(),
            EnableProviderPayCdi = new LaunchDarklyFlagConfig(),
            EnableOrderCreation = new LaunchDarklyFlagConfig(),
            EnableBillAccepted = new LaunchDarklyFlagConfig(),
            EnableKedLabResultIngestion = new LaunchDarklyFlagConfig(),
            EnableDirectBilling = new LaunchDarklyFlagConfig()
        };
        var mockLdClientConfig = Configuration.Builder("FAKE_SDK_KEY")
            .Offline(true)
            .Build();
        var featureFlags = new FeatureFlags(mockLdConfig, new LdClient(mockLdClientConfig),
            new LdClient(mockLdClientConfig));

        mockLdConfig.EnableEgfrLabResultsIngestion.FlagDefault = flagValue;
        mockLdConfig.EnableProviderPayCdi.FlagDefault = flagValue;
        mockLdConfig.EnableOrderCreation.FlagDefault = flagValue;
        mockLdConfig.EnableBillAccepted.FlagDefault = flagValue;
        mockLdConfig.EnableKedLabResultIngestion.FlagDefault = flagValue;
        mockLdConfig.EnableDirectBilling.FlagDefault = flagValue;

        //ACT
        var enableEgfrLabResultsIngestion = featureFlags.EnableEgfrLabResultsIngestion;
        var enableProviderPayCdi = featureFlags.EnableProviderPayCdi;
        var enableOrderCreation = featureFlags.EnableOrderCreation;
        var enableBillAccepted = featureFlags.EnableBillAccepted;
        var enableKedLabResultIngestion = featureFlags.EnableKedLabResultIngestion;
        var enableDirectBilling = featureFlags.EnableDirectBilling;

        //ASSERT
        Assert.Equal(flagValue, enableEgfrLabResultsIngestion);
        Assert.Equal(flagValue, enableProviderPayCdi);
        Assert.Equal(flagValue, enableOrderCreation);
        Assert.Equal(flagValue, enableBillAccepted);
        Assert.Equal(flagValue, enableKedLabResultIngestion);
        Assert.Equal(flagValue, enableDirectBilling);
    }
}