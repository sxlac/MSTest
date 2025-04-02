using LaunchDarkly.Sdk.Server;
using Signify.eGFR.Core.Configs;

namespace Signify.eGFR.Core.FeatureFlagging;

public class FeatureFlags : FeatureFlagsBase, IFeatureFlags
{
    public FeatureFlags(LaunchDarklyConfig ldConfig) : base(ldConfig)
    {
    }

    public FeatureFlags(LaunchDarklyConfig ldConfig, LdClient sharedClient, LdClient projectClient) : base(ldConfig,
        sharedClient, projectClient)
    {
    }

    // Feature Flags
    public virtual bool EnableEgfrLabResultsIngestion => Evaluate(LdConfig.EnableEgfrLabResultsIngestion);
    public virtual bool EnableProviderPayCdi => Evaluate(LdConfig.EnableProviderPayCdi);
    public virtual bool EnableOrderCreation => Evaluate(LdConfig.EnableOrderCreation);
    public virtual bool EnableBillAccepted => Evaluate(LdConfig.EnableBillAccepted);
    public virtual bool EnableKedLabResultIngestion => Evaluate(LdConfig.EnableKedLabResultIngestion);
    public virtual bool EnableDirectBilling => Evaluate(LdConfig.EnableDirectBilling);
    public virtual bool EnableInternalLabResultIngestion => Evaluate(LdConfig.EnableInternalLabResultIngestion);
}