using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.Spirometry.Core.Configs;

/// <summary>
/// Configs for health checks for Uris
/// </summary>
[ExcludeFromCodeCoverage]
public class UriHealthCheckConfig
{
    /// <summary>
    /// Configuration section key for this config
    /// </summary>
    public const string Key = "UriHealthChecks";

    /// <summary>
    /// Name of the Uri/API
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Uri to monitor
    /// </summary>
    public Uri Uri { get; set; }

    /// <summary>
    /// How long to wait for a response before considering the health check unhealthy
    /// </summary>
    public int TimeoutMs { get; set; } = 6_000;
}