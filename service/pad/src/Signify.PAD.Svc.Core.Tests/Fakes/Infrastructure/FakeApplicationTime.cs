using Signify.PAD.Svc.Core.Infrastructure;
using System;

namespace Signify.PAD.Svc.Core.Tests.Fakes.Infrastructure;

/// <summary>
/// Fake application time that always returns the same timestamp for <see cref="UtcNow"/>, to aide in unit testing
/// </summary>
public class FakeApplicationTime : IApplicationTime
{
    private readonly DateTime _now = new(2022, 2, 3, 4, 5, 6, DateTimeKind.Utc);

    /// <inheritdoc />
    public DateTime UtcNow()
    {
        return _now;
    }
}