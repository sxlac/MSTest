using Signify.Spirometry.Core.Exceptions;
using System.Net;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Exceptions;

public class GetEvaluationsExceptionTests
{
    [Fact]
    public void Constructor_SetsProperties_Test()
    {
        // Arrange
        const long memberPlanId = 1;
        const long appointmentId = 2;
        const HttpStatusCode statusCode = HttpStatusCode.InternalServerError;

        // Act
        var ex = new GetEvaluationsException(memberPlanId, appointmentId, statusCode, default);

        // Assert
        Assert.Equal(memberPlanId, ex.MemberPlanId);
        Assert.Equal(appointmentId, ex.AppointmentId);
        Assert.Equal(statusCode, ex.StatusCode);
    }

    [Fact]
    public void Constructor_SetsMessage_Test()
    {
        // Arrange
        const long memberPlanId = 1;
        const long appointmentId = 2;
        const HttpStatusCode statusCode = HttpStatusCode.InternalServerError;
        const string message = "some message";

        // Act
        var ex = new GetEvaluationsException(memberPlanId, appointmentId, statusCode, message);

        // Assert
        Assert.Equal("some message for MemberPlanId=1, from AppointmentId=2, with StatusCode=InternalServerError", ex.Message);
    }
}