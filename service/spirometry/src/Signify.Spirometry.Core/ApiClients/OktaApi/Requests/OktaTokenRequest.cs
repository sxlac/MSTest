using System.Diagnostics.CodeAnalysis;

namespace Signify.Spirometry.Core.ApiClients.OktaApi.Requests;

[ExcludeFromCodeCoverage]
public class OktaTokenRequest
{
    public string grant_type { get; set; } = "client_credentials";

    public string scope { get; set; }

    public override string ToString()
    {
        return $"{nameof(grant_type)}={grant_type}&{nameof(scope)}={scope}";
    }
}