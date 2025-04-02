using LaunchDarkly.Sdk.Server;
using Signify.CKD.Svc.Core.Configs;
using Signify.CKD.Svc.Core.FeatureFlagging;
using Xunit;

namespace Signify.CKD.Svc.Core.Tests.FeatureFlagging;

public class FeatureFlagsTests
{
    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void FeatureFlag_WhenDefaulted_EvaluatesToDefaulted(bool flagValue, bool compareValue)
    {
        //ARRANGE
        var mockLdConfig = new LaunchDarklyConfig { EnableProviderPayCdi = new LaunchDarklyFlagConfig() };
        var mockLdClientConfig = Configuration.Builder("FAKE_SDK_KEY")
            .Offline(true)
            .Build();
        var featureFlags = new FeatureFlags(mockLdConfig, new LdClient(mockLdClientConfig), new LdClient(mockLdClientConfig));
        
        mockLdConfig.EnableProviderPayCdi.FlagDefault = flagValue;
        
        //ACT
        var result = featureFlags.EnableProviderPayCdi;
        
        //ASSERT
        Assert.Equal(result,compareValue);
    }
}