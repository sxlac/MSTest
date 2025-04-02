using System;
using System.Collections.Generic;

namespace Signify.eGFR.Core.Configs;

public class OktaConfig
{
    public const string Key = "Okta";
    public IEnumerable<string> Scopes { get; set; }
    public Uri Domain { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
}