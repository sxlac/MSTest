using System;

namespace Signify.HBA1CPOC.Svc.Core.Infrastructure;

/// <summary>
/// Implementation of <see cref="IApplicationTime"/>
/// </summary>
public class ApplicationTime : IApplicationTime
{
    ///<inheritdoc />
    public DateTime UtcNow()
    {
        return DateTime.UtcNow;
    }
}