namespace Signify.uACR.Core.FeatureFlagging;

public interface IFeatureFlags
{
    bool EnableOrderCreation { get; }

    bool EnableProviderPayCdi { get; }

    bool EnableBilling { get; }

    bool EnableLabResultIngestion { get; }
    
    bool EnableBillAccepted { get; }
    
    bool EnableDirectBilling { get; }

    bool EnableInternalLabResultIngestion { get; }
    bool EnableDlq { get; }
}