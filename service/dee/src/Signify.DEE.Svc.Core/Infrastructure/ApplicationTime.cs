using System;

namespace Signify.DEE.Svc.Core.Infrastructure;

public class ApplicationTime : IApplicationTime
{
    public DateTime UtcNow() => DateTime.UtcNow;
}