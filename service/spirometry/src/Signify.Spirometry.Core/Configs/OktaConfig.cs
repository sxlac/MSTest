using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Signify.Spirometry.Core.Configs;

/// <summary>
/// Configs for Okta
/// </summary>
[ExcludeFromCodeCoverage]
public class OktaConfig
{
    /// <summary>
    /// Configuration section key for this config
    /// </summary>
    public const string Key = "Okta";

    /// <summary>
    /// Domain uri for Okta
    /// </summary>
    public Uri Domain { get; set; }

    /// <summary>
    /// API service account username
    /// </summary>
    public string ClientId { get; set; }

    /// <summary>
    /// Password for the service account
    /// </summary>
    public string ClientSecret { get; set; }

    /// <summary>
    /// Scopes the service needs (ex the different APIs the service needs to make requests to)
    /// </summary>
    public IEnumerable<string> Scopes { get; set; }
}