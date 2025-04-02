using Signify.FOBT.Svc.Core.Exceptions;
using System.Collections.Generic;
using System.Net;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.Exceptions;

public class CreateOrderExceptionTests
{
    [Theory]
    [MemberData(nameof(Constructor_SetsProperties_TestData))]
    public void Constructor_SetsProperties_Tests(long evaluationId, HttpStatusCode statusCode)
    {
        var ex = new CreateOrderException( evaluationId, statusCode);

        Assert.Equal(evaluationId, ex.EvaluationId);
        Assert.Equal(statusCode, ex.StatusCode);
    }

    public static IEnumerable<object[]> Constructor_SetsProperties_TestData()
    {
        yield return
        [
            1,
            HttpStatusCode.InternalServerError
        ];

        yield return
        [
            long.MaxValue,
            HttpStatusCode.BadRequest
        ];
    }
}