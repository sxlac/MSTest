namespace Signify.DEE.Svc.System.Tests;

[TestClass]
public class TearDown
{
    public TestContext TestContext { get; set; }
    
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