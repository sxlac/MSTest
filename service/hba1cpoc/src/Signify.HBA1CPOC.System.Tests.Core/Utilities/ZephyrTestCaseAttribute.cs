namespace Signify.HBA1CPOC.System.Tests.Core.Utilities;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
public class ZephyrTestCaseAttribute : Attribute
{
    public string TestCaseKey { get; private set; }

    public ZephyrTestCaseAttribute(string testCaseKey)
    {
        TestCaseKey = testCaseKey;
    }
}