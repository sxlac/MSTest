using LaunchDarkly.Sdk.Server;
using Signify.uACR.Core.Configs;

namespace Signify.uACR.Core.FeatureFlagging;

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
    public virtual bool EnableOrderCreation => Evaluate(LdConfig.EnableOrderCreation);
    public virtual bool EnableProviderPayCdi => Evaluate(LdConfig.EnableProviderPayCdi);
    public virtual bool EnableBilling => Evaluate(LdConfig.EnableBilling);
    public virtual bool EnableLabResultIngestion => Evaluate(LdConfig.EnableLabResultIngestion);
    public virtual bool EnableBillAccepted => Evaluate(LdConfig.EnableBillAccepted);
    public virtual bool EnableDirectBilling => Evaluate(LdConfig.EnableDirectBilling);
    public virtual bool EnableInternalLabResultIngestion => Evaluate(LdConfig.EnableInternalLabResultIngestion);
    public virtual bool EnableDlq => Evaluate(LdConfig.EnableDlq);
}