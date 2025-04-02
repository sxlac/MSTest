using LaunchDarkly.Sdk.Server;
using Signify.CKD.Svc.Core.Configs;

namespace Signify.CKD.Svc.Core.FeatureFlagging;

public class FeatureFlags: FeatureFlagsBase, IFeatureFlags
{
    public FeatureFlags(LaunchDarklyConfig ldConfig) : base(ldConfig) { }
    public FeatureFlags(LaunchDarklyConfig ldConfig, LdClient sharedClient, LdClient projectClient) : base(ldConfig, sharedClient, projectClient) { }
    
    public virtual bool EnableProviderPayCdi => Evaluate(LdConfig.EnableProviderPayCdi);
}