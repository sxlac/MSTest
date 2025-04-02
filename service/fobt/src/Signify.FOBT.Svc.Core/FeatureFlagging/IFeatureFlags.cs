namespace Signify.FOBT.Svc.Core.FeatureFlagging;

public interface IFeatureFlags
{
    bool EnableDlq { get; }
}