namespace Signify.HBA1CPOC.System.Tests;

public class CustomTestMethodAttribute : TestMethodAttribute
{

    public int MaxRetries { get; set; } = 5;

    public override TestResult[] Execute(ITestMethod testMethod)
    {
        var count = 0;
        TestResult[] result = null;
        while (count <= MaxRetries)
        {
            try
            {
                result = base.Execute(testMethod);
                if (result[0].TestFailureException != null)
                {
                    throw result[0].TestFailureException;
                }
                break;
            }
            catch (Exception e)
            {
                Console.WriteLine(count == MaxRetries
                    ? $"Test {testMethod.TestMethodName} failed on attempt {count}. Retries exhausted.\n{e}"
                    : $"Test {testMethod.TestMethodName} failed on attempt {count}. Retrying...\n{e}");
            }
            finally
            {
                count++;
            }
        }
        return result;
    }

}