using System.Diagnostics.CodeAnalysis;

namespace Signify.DEE.Svc.Core.Infrastructure;

[ExcludeFromCodeCoverage]
public class AccessToken
{
    public string access_token { get; set; }
    public string token_type { get; set; }
    public int expires_in { get; set; }

    public override string ToString()
    {
        return $"{nameof(access_token)}: {access_token}, {nameof(token_type)}: {token_type}, {nameof(expires_in)}: {expires_in}";
    }

    protected bool Equals(AccessToken other)
    {
        return string.Equals(access_token, other.access_token) && string.Equals(token_type, other.token_type) && expires_in == other.expires_in;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((AccessToken) obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = (access_token != null ? access_token.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (token_type != null ? token_type.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ expires_in;
            return hashCode;
        }
    }
}