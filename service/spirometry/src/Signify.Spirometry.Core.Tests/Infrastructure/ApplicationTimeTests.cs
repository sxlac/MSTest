using Signify.Spirometry.Core.Infrastructure;
using System;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Infrastructure;

public class ApplicationTimeTests
{
    private static ApplicationTime CreateSubject() => new();

    [Fact]
    public void UtcNow_Test()
    {
        // Arrange
        var subject = CreateSubject();

        // Act
        var now = DateTime.UtcNow;
        var actual = subject.UtcNow();

        // Assert
        Assert.Equal(DateTimeKind.Utc, actual.Kind);

        Assert.True(actual >= now);

        const double threshold = 2;

        Assert.True((actual - now).TotalMilliseconds < threshold);
    }
}