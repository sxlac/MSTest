using System;

namespace Signify.FOBT.Svc.Core.Infrastructure;

/// <summary>
/// Implementation of <see cref="IApplicationTime"/>
/// </summary>
public class ApplicationTime : IApplicationTime
{
    private readonly TimeProvider _timeProvider;

    public ApplicationTime(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    ///<inheritdoc />
    public DateTime UtcNow()
        => _timeProvider.GetUtcNow().UtcDateTime;
}
