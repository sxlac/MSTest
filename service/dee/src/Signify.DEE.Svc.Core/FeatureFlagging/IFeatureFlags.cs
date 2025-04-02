namespace Signify.DEE.Svc.Core.FeatureFlagging;

public interface IFeatureFlags
{
    bool EnableProviderPayCdi { get; }
    bool EnableBillAccepted { get; }
    bool EnableDlq { get; }
}