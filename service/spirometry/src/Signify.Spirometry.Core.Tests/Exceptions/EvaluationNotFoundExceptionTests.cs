using Signify.Spirometry.Core.Exceptions;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Exceptions;

public class EvaluationNotFoundExceptionTests
{
    [Fact]
    public void Constructor_SetsProperties_Test()
    {
        // Arrange
        const long appointmentId = 1;

        // Act
        var ex = new EvaluationNotFoundException(appointmentId);

        // Assert
        Assert.Equal(appointmentId, ex.AppointmentId);
    }

    [Fact]
    public void Constructor_SetsMessage_Test()
    {
        // Arrange
        const long appointmentId = 1;

        // Act
        var ex = new EvaluationNotFoundException(appointmentId);

        // Assert
        Assert.Equal("Unable to find an evaluation for AppointmentId=1", ex.Message);
    }
}