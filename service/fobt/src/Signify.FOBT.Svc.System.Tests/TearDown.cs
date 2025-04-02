using Signify.QE.Core.Actions;

namespace Signify.FOBT.Svc.System.Tests;

[TestClass]
public class TearDown
{
    
    [AssemblyCleanup]
    public static async Task TestTeardown()
    {
        StopKafkaConsumer();
    }
    private static async Task StopKafkaConsumer()
    {
        try
        {
            var kafkaActions = new KafkaActions(KafkaConfig);
            await kafkaActions.StopConsumer();
        }
        catch (NullReferenceException)
        {
            // Do nothing because the Kafka consumer was never started
        }
    }
    
}