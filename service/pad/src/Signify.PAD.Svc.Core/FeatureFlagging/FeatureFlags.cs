using LaunchDarkly.Sdk.Server;
using Signify.PAD.Svc.Core.Configs;

namespace Signify.PAD.Svc.Core.FeatureFlagging;

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