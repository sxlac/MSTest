namespace Signify.Spirometry.Core.FeatureFlagging;

public interface IFeatureFlags
{
    bool EnableBillAccepted { get; }
    bool EnableDlq { get; }
}