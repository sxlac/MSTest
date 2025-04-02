using System.Collections.Generic;
using System.Net;
using Signify.CKD.Svc.Core.Exceptions;
using Xunit;

namespace Signify.CKD.Svc.Core.Tests.Exceptions
{
    public class RcmBillingRequestExceptionTests
    {
        [Theory]
        [MemberData(nameof(Constructor_SetsProperties_TestData))]
        public void Constructor_SetsProperties_Tests(long evaluationId, HttpStatusCode statusCode)
        {
            var ex = new RcmBillingRequestException(evaluationId, statusCode, default);

            Assert.Equal(evaluationId, ex.EvaluationId);
            Assert.Equal(statusCode, ex.StatusCode);
        }

        public static IEnumerable<object[]> Constructor_SetsProperties_TestData()
        {
            yield return new object[]
            {
                1,
                HttpStatusCode.InternalServerError
            };

            yield return new object[]
            {
                long.MaxValue,
                HttpStatusCode.BadRequest
            };
        }

        [Fact]
        public void Constructor_SetsMessage_Test()
        {
            var ex = new RcmBillingRequestException(1, HttpStatusCode.BadRequest, "message");

            Assert.Equal("message for EvaluationId=1, with StatusCode=BadRequest", ex.Message);
        }
    }
}
