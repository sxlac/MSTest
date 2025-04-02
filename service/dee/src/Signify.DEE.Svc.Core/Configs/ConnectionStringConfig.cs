using System.Diagnostics.CodeAnalysis;

namespace Signify.DEE.Svc.Core.Configs;

[ExcludeFromCodeCoverage]
public class ConnectionStringConfig
{
    public string DB { get; set; }
    public string AzureServiceBus { get; set; }
    public string IrisResultDeliveryServiceBus { get; set; }
}