namespace Signify.HBA1CPOC.System.Tests;

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
        BaseTestActions.CoreKafkaActions.StopConsumer();
    }
}