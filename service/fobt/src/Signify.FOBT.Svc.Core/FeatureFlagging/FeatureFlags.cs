using LaunchDarkly.Sdk.Server;
using Signify.FOBT.Svc.Core.Configs;

namespace Signify.FOBT.Svc.Core.FeatureFlagging;

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
    public virtual bool EnableDlq => Evaluate(LdConfig.EnableDlq);
}