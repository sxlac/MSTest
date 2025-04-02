using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.eGFR.Core.Infrastructure;

[ExcludeFromCodeCoverage]
public class ApplicationTime : IApplicationTime
{
    public DateTime UtcNow()
        => DateTime.UtcNow;
}