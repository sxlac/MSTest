using Signify.QE.Core.Actions;
using Signify.Spirometry.Svc.System.Tests.Core.Actions;

namespace Signify.Spirometry.Svc.System.Tests;

[TestClass]
public class TearDown
{
    
    [AssemblyCleanup]
    public static async Task TestTeardown()
    {
        await StopKafkaConsumer();
    }
    private static async Task StopKafkaConsumer()
    {
        try
        {
            await BaseTestActions.CoreKafkaActions.StopConsumer();
        }
        catch (NullReferenceException)
        {
            // Do nothing because the Kafka consumer was never started
        }
    }
    
}