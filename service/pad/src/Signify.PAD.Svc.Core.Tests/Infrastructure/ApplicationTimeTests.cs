using Signify.PAD.Svc.Core.Infrastructure;
using System;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Infrastructure;

public class ApplicationTimeTests
{
    [Fact]
    public void UtcNow_Returns_Now()
    {
        var now = DateTime.UtcNow;

        var subject = new ApplicationTime();

        var actual = subject.UtcNow();

        Assert.True(actual - now < TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public void UtcNow_Returns_DateTimeKindUtc()
    {
        var subject = new ApplicationTime();

        var actual = subject.UtcNow().Kind;

        Assert.Equal(DateTimeKind.Utc, actual);
    }
}