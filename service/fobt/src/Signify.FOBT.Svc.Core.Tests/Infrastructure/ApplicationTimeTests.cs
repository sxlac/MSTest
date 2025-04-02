using Microsoft.Extensions.Time.Testing;
using Signify.FOBT.Svc.Core.Infrastructure;
using System;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.Infrastructure;

public class ApplicationTimeTests
{
    private static ApplicationTime CreateSubject(TimeProvider timeProvider)
        => new(timeProvider);

    [Fact]
    public void UtcNow_Returns_Now()
    {
        // Arrange
        var now = DateTime.UtcNow;

        var timeProvider = new FakeTimeProvider();

        timeProvider.SetUtcNow(now);

        var subject = CreateSubject(timeProvider);

        // Act
        var actual = subject.UtcNow();

        // Assert
        Assert.Equal(now, actual);
    }

    [Fact]
    public void UtcNow_Returns_DateTimeKindUtc()
    {
        var subject = CreateSubject(TimeProvider.System);

        var actual = subject.UtcNow().Kind;

        Assert.Equal(DateTimeKind.Utc, actual);
    }
}
