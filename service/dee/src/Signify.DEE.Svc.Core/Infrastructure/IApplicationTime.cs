using System;

namespace Signify.DEE.Svc.Core.Infrastructure;

/// <summary>
/// Interface to get the current time from the application. Primarily to help in unit testing, but may be
/// expanded upon in the future.
/// </summary>
public interface IApplicationTime
{
    /// <summary>
    /// Gets a <see cref="DateTime"/> object that is set to the current time, expressed in UTC
    /// </summary>
    /// <returns></returns>
    DateTime UtcNow();
}