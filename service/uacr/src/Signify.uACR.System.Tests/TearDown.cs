namespace Signify.uACR.System.Tests;

[TestClass]
public class TearDown
{
    [AssemblyCleanup]
    public static void TestTeardown()
    {
        StopKafkaConsumer();
    }

    private static void StopKafkaConsumer()
    {
        try
        {
            BaseTestActions.CoreKafkaActions.StopConsumer();
        }
        catch (NullReferenceException)
        {
            // Do nothing because the Kafka consumer was never started
        }
    }
}