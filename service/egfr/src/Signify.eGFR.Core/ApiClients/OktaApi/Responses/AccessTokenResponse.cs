namespace Signify.eGFR.Core.ApiClients.OktaApi.Responses;

/// <summary>
/// Response type for performing a POST to the token endpoint on the <see cref="IOktaApi"/>
/// </summary>
public class AccessTokenResponse
{
    public string access_token { get; set; }

    public int expires_in { get; set; }
}