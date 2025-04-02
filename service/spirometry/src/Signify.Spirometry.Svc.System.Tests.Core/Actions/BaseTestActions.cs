using Signify.Dps.Test.Utilities.CoreApi.Actions;
using Signify.Dps.Test.Utilities.Kafka.Actions;

namespace Signify.Spirometry.Svc.System.Tests.Core.Actions;

public class BaseTestActions : DatabaseActions
{
    protected CoreApiActions CoreApiActions = new (CoreApiConfigs, Provider.ProviderId, Product, FormVersionId, LoggingHttpMessageHandler);
    public static CoreKafkaActions CoreKafkaActions; 
    
}