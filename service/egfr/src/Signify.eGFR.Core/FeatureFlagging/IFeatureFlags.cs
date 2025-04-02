namespace Signify.eGFR.Core.FeatureFlagging;

public interface IFeatureFlags
{
    bool EnableEgfrLabResultsIngestion { get; }
    bool EnableProviderPayCdi { get; }

    bool EnableOrderCreation { get; }
    bool EnableBillAccepted { get; }

    bool EnableKedLabResultIngestion { get; }
    
    bool EnableDirectBilling { get; }
    bool EnableInternalLabResultIngestion { get; }
}