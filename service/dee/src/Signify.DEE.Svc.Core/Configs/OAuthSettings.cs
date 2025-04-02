using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Signify.DEE.Svc.Core.Configs;

[ExcludeFromCodeCoverage]
public class OAuthSettings
{
    public string OAuthUrl { get; set; }
    public string ClientName { get; set; }
    public string ClientPassword { get; set; }
    public string Audience { get; set; }
    public string UserName { get; set; }
    public string UserPassword { get; set; }
    public List<string> Scopes { get; set; }

    public string AccessToken { get; set; }

    public override string ToString()
    {
        return $"{nameof(OAuthUrl)}: {OAuthUrl}, {nameof(ClientName)}: {ClientName}, {nameof(ClientPassword)}: {ClientPassword}, {nameof(Audience)}: {Audience}, {nameof(UserName)}: {UserName}, {nameof(UserPassword)}: {UserPassword}, {nameof(Scopes)}: {Scopes}, {nameof(AccessToken)}: {AccessToken}";
    }

    protected bool Equals(OAuthSettings other)
    {
        return string.Equals(OAuthUrl, other.OAuthUrl) && string.Equals(ClientName, other.ClientName) && string.Equals(ClientPassword, other.ClientPassword) && string.Equals(Audience, other.Audience) && string.Equals(UserName, other.UserName) && string.Equals(UserPassword, other.UserPassword) && Equals(Scopes, other.Scopes) && string.Equals(AccessToken, other.AccessToken);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((OAuthSettings) obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = (OAuthUrl != null ? OAuthUrl.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (ClientName != null ? ClientName.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (ClientPassword != null ? ClientPassword.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (Audience != null ? Audience.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (UserName != null ? UserName.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (UserPassword != null ? UserPassword.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (Scopes != null ? Scopes.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (AccessToken != null ? AccessToken.GetHashCode() : 0);
            return hashCode;
        }
    }
}