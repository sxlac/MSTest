using Signify.Spirometry.Core.Exceptions;
using System.Net;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Exceptions;

public class GetAppointmentExceptionTests
{
    [Fact]
    public void Constructor_SetsProperties_Test()
    {
        // Arrange
        const long appointmentId = 1;
        const HttpStatusCode statusCode = HttpStatusCode.InternalServerError;

        // Act
        var ex = new GetAppointmentException(appointmentId, statusCode, default);

        // Assert
        Assert.Equal(appointmentId, ex.AppointmentId);
        Assert.Equal(statusCode, ex.StatusCode);
    }

    [Fact]
    public void Constructor_SetsMessage_Test()
    {
        // Arrange
        const long appointmentId = 1;
        const HttpStatusCode statusCode = HttpStatusCode.InternalServerError;
        const string message = "some message";

        // Act
        var ex = new GetAppointmentException(appointmentId, statusCode, message);

        // Assert
        Assert.Equal("some message for AppointmentId=1, with StatusCode=InternalServerError", ex.Message);
    }
}