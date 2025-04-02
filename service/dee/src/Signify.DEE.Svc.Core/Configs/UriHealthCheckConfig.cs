using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.DEE.Svc.Core.Configs;

[ExcludeFromCodeCoverage]
public class UriHealthCheckConfig
{
    public string Name { get; private set; }
    public Uri Uri { get; private set; }
        
    public int TimeoutMs { get; private set; }
}