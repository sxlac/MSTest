namespace Signify.PAD.Svc.Core.FeatureFlagging;

public interface IFeatureFlags
{
    bool EnableDlq { get; }
}