using System.Diagnostics.CodeAnalysis;
using LaunchDarkly.Sdk.Server;
using Signify.DEE.Svc.Core.Configs;

namespace Signify.DEE.Svc.Core.FeatureFlagging;

public class FeatureFlags : FeatureFlagsBase, IFeatureFlags
{
    [ExcludeFromCodeCoverage]
    public FeatureFlags(LaunchDarklyConfig ldConfig) : base(ldConfig)
    {
    }

    public FeatureFlags(LaunchDarklyConfig ldConfig, LdClient sharedClient, LdClient projectClient) : base(ldConfig,
        sharedClient, projectClient)
    {
    }

    // Feature Flags
    public virtual bool EnableProviderPayCdi => Evaluate(LdConfig.EnableProviderPayCdi);
    public virtual bool EnableBillAccepted => Evaluate(LdConfig.EnableBillAccepted);
    public virtual bool EnableDlq => Evaluate(LdConfig.EnableDlq);
}