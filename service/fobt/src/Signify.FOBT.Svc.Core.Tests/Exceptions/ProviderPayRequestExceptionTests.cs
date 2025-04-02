using Signify.FOBT.Svc.Core.Exceptions;
using System.Collections.Generic;
using System.Net;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.Exceptions;

public class ProviderPayRequestExceptionTests
{
    [Theory]
    [MemberData(nameof(Constructor_SetsProperties_TestData))]
    public void Constructor_SetsProperties_Tests(int fobtId, long evaluationId, HttpStatusCode statusCode)
    {
        var ex = new ProviderPayRequestException(fobtId, evaluationId, statusCode, default);

        Assert.Equal(fobtId, ex.FobtId);
        Assert.Equal(evaluationId, ex.EvaluationId);
        Assert.Equal(statusCode, ex.StatusCode);
    }

    public static IEnumerable<object[]> Constructor_SetsProperties_TestData()
    {
        yield return
        [
            1,
            1,
            HttpStatusCode.InternalServerError
        ];

        yield return
        [
            int.MaxValue,
            long.MaxValue,
            HttpStatusCode.BadRequest
        ];
    }
}