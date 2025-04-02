using Signify.PAD.Svc.Core.Exceptions;
using System.Net;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Exceptions;

public class ProviderPayRequestExceptionTests
{
    [Fact]
    public void Constructor_SetsProperties_Test()
    {
        const int padId = 1;
        const long evaluationId = 1;
        const HttpStatusCode httpStatusCode = HttpStatusCode.Unauthorized;
        const string message = "Error has occured";

        var ex = new ProviderPayRequestException(padId, evaluationId, httpStatusCode, message);

        Assert.Equal(padId, ex.PadId);
        Assert.Equal(evaluationId, ex.EvaluationId);
        Assert.Equal(httpStatusCode, ex.StatusCode);
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
        const int padId = 1;
        const long evaluationId = 1;
        const string message = "Error has occured";

        var ex = new ProviderPayRequestException(padId, evaluationId, statusCode, message);

        Assert.Equal($"{message} for PadId={padId}, EvaluationId={evaluationId}, with StatusCode={statusCode}", ex.Message);
    }
}