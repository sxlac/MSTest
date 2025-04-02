using Signify.PAD.Svc.Core.Exceptions;
using System.Net;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Exceptions;

public class EvaluationApiRequestExceptionTests
{
    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.MovedPermanently)]
    public void Constructor_SetsProperties_Test(HttpStatusCode statusCode)
    {
        const long evaluationId = 1;
        const string message = "Error with the payload";
        
        var ex = new EvaluationApiRequestException(evaluationId, statusCode, message);

        Assert.Equal(evaluationId, ex.EvaluationId);
        Assert.Equal(statusCode, ex.StatusCode);
        Assert.Equal(message, ex.ErrorMessage);
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

        var ex = new EvaluationApiRequestException(evaluationId, statusCode, message);

        Assert.Equal($"Error with the payload for EvaluationId=1, with StatusCode={statusCode}", ex.Message);
    }
}