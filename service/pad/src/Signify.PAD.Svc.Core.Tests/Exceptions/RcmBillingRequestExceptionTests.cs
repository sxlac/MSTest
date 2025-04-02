using Signify.PAD.Svc.Core.Exceptions;
using System.Net;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Exceptions;

public class RcmBillingRequestExceptionTests
{
    [Fact]
    public void Constructor_SetsProperties_Test()
    {
        const long evaluationId = 1;
        const HttpStatusCode statusCode = HttpStatusCode.Forbidden;
        const string message = "Error with the payload";
        
        var ex = new RcmBillingRequestException(evaluationId, statusCode, message);

        Assert.Equal(evaluationId, ex.EvaluationId);
        Assert.Equal(statusCode, ex.StatusCode);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.Accepted)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.MovedPermanently)]
    public void Constructor_SetsMessage_Test(HttpStatusCode statusCode)
    {
        const long evaluationId = 1;
        const string message = "Error with the payload";

        var ex = new RcmBillingRequestException(evaluationId, statusCode, message);

        Assert.Equal($"{message} for EvaluationId={evaluationId}, with StatusCode={statusCode}", ex.Message);
    }
}