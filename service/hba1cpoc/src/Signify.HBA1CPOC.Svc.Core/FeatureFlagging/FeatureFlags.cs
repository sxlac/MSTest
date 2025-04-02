using LaunchDarkly.Sdk.Server;
using Signify.HBA1CPOC.Svc.Core.Configs;

namespace Signify.HBA1CPOC.Svc.Core.FeatureFlagging;

public class FeatureFlags: FeatureFlagsBase, IFeatureFlags
{
    public FeatureFlags(LaunchDarklyConfig ldConfig) : base(ldConfig) { }
    public FeatureFlags(LaunchDarklyConfig ldConfig, LdClient sharedClient, LdClient projectClient) : base(ldConfig, sharedClient, projectClient) { }
}