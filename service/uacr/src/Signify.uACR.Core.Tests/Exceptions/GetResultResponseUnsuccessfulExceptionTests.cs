using System.Net;
using Signify.uACR.Core.Exceptions;
using Xunit;

namespace Signify.uACR.Core.Tests.Exceptions;

public class GetResultResponseUnsuccessfulExceptionTests
{
    [Fact]
    public void Constructor_SetsProperties_Test()
    {
        var labResultId = System.Security.Cryptography.RandomNumberGenerator.GetInt32(123456, 1234567891);
        const string vendor = "abc";
        const string testName = "def";

        var ex = new GetResultResponseUnsuccessfulException(labResultId, vendor, testName, HttpStatusCode.Unauthorized);

        Assert.Equal(labResultId, ex.LabResultId);
        Assert.Equal(vendor, ex.Vendor);
        Assert.Equal(testName, ex.TestName);
        Assert.Equal(HttpStatusCode.Unauthorized, ex.HttpStatusCode);
    }

    [Fact]
    public void Constructor_SetsMessage_Test()
    {
        var labResultId = System.Security.Cryptography.RandomNumberGenerator.GetInt32(123456, 1234567891);
        const string vendor = "abc";
        const string testName = "def";

        var ex = new GetResultResponseUnsuccessfulException(labResultId, vendor, testName, HttpStatusCode.Accepted);


        Assert.Equal($"Unsuccessful Http Response for LabResultId Id:{labResultId}, Vendor:{vendor}, Test:{testName}, HttpStatusCode:{HttpStatusCode.Accepted}", ex.Message);
    }
}