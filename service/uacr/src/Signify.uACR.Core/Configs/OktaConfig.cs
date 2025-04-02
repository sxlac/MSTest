using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System;

namespace Signify.uACR.Core.Configs;

[ExcludeFromCodeCoverage]
public class OktaConfig
{
    public const string Key = "Okta";
    public IEnumerable<string> Scopes { get; set; }
    public Uri Domain { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
}