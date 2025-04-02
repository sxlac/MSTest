using LaunchDarkly.Sdk.Server;
using Signify.Spirometry.Core.Configs;

namespace Signify.Spirometry.Core.FeatureFlagging;

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
    
    public virtual bool EnableBillAccepted => Evaluate(LdConfig.EnableBillAccepted);
    public virtual bool EnableDlq => Evaluate(LdConfig.EnableDlq);
}