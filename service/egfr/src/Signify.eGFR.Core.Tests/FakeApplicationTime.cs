using Signify.eGFR.Core.Infrastructure;
using System;

namespace Signify.eGFR.Core.Tests;

/// <summary>
/// Fake application time that always returns the same timestamp for <see cref="UtcNow"/>
/// </summary>
public class FakeApplicationTime : IApplicationTime
{
    private readonly DateTime _now = new(2022, 2, 3, 4, 5, 6, DateTimeKind.Utc);
    private readonly DateTime _localNow = new(2022, 2, 3, 4, 5, 6, DateTimeKind.Unspecified);

    public DateTime UtcNow()
    {
        return _now;
    }
    
    /// <summary>
    /// Get datetime with -05:00 as the offset
    /// Note: Not including this as part of IApplicationTime as this is used for testing alone
    /// </summary>
    /// <returns></returns>
    public DateTimeOffset LocalNow()
    {
        return new DateTimeOffset(_localNow,  new TimeSpan(-5, 0, 0));
    }
}