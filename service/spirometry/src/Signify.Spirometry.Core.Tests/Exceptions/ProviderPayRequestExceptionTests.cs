using Signify.Spirometry.Core.Exceptions;
using System.Collections.Generic;
using System.Net;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Exceptions;

public class ProviderPayRequestExceptionTests
{
    [Theory]
    [MemberData(nameof(Constructor_SetsProperties_TestData))]
    public void Constructor_SetsProperties_Tests(int examId, long evaluationId, HttpStatusCode statusCode)
    {
        var ex = new ProviderPayRequestException(examId, evaluationId, statusCode, default);

        Assert.Equal(examId, ex.ExamId);
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