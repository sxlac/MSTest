using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Polly;
using Signify.Spirometry.Core.ApiClients.OktaApi;
using Signify.Spirometry.Core.ApiClients.OktaApi.Requests;
using Signify.Spirometry.Core.Configs;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.Spirometry.Core.Infrastructure;

[ExcludeFromCodeCoverage]
public class OktaClientCredentialsHttpClientHandler : DelegatingHandler
{
    private const string Key = "OktaClientAccessToken";

    private readonly ILogger _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly IApplicationTime _applicationTime;
    private readonly IOktaApi _oktaApi;
    private readonly OktaConfig _oktaConfig;

    public OktaClientCredentialsHttpClientHandler(ILogger<OktaClientCredentialsHttpClientHandler> logger,
        IMemoryCache memoryCache,
        IApplicationTime applicationTime,
        IOktaApi oktaApi,
        OktaConfig oktaConfig)
    {
        _logger = logger;
        _memoryCache = memoryCache;
        _applicationTime = applicationTime;
        _oktaApi = oktaApi;
        _oktaConfig = oktaConfig;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        async Task<AuthenticationHeaderValue> GetAuthorizationHeaderValue()
        {
            var authnHeader = request.Headers.Authorization;
            var token = await GetDeviceToken();
            return new AuthenticationHeaderValue(authnHeader?.Scheme ?? "Bearer", token);
        }

        request.Headers.Authorization = await GetAuthorizationHeaderValue();

        return await Policy
            .HandleResult<HttpResponseMessage>(response => response.StatusCode == HttpStatusCode.Unauthorized)
            .RetryAsync(1, async (_, _) =>
            {
                _memoryCache.Remove(Key);
                request.Headers.Authorization = await GetAuthorizationHeaderValue();
            })
            .ExecuteAsync(() => base.SendAsync(request, cancellationToken));
    }

    private async Task<string> GetDeviceToken()
    {
        return await _memoryCache.GetOrCreateAsync(Key, GetToken);

        async Task<string> GetToken(ICacheEntry cacheEntry)
        {
            var authorizationHeader = Base64Encode(_oktaConfig.ClientId, _oktaConfig.ClientSecret);

            var now = _applicationTime.UtcNow();

            var request = new OktaTokenRequest
            {
                scope = string.Join(' ', _oktaConfig.Scopes)
            };

            var response = await _oktaApi.GetAccessToken(authorizationHeader, "application/x-www-form-urlencoded", request.ToString());
            if (response.IsSuccessStatusCode)
            {
                if (response.Content.expires_in > 0)
                    cacheEntry.AbsoluteExpiration = now.AddSeconds(response.Content.expires_in);

                return response.Content.access_token;
            }

            _logger.LogError("Unable to authenticate Okta access token. StatusCode={StatusCode}, Reason={Reason}, Request={Request}",
                response.StatusCode, response.ReasonPhrase, request);

            if (response.Error != null)
                throw response.Error;

            throw new HttpRequestException("Failed to obtain access token from Okta");
        }
    }

    private static string Base64Encode(string username, string password)
    {
        var auth = $"{username}:{password}";
        var plainTextBytes = Encoding.UTF8.GetBytes(auth);
        var encodedCredentials = Convert.ToBase64String(plainTextBytes);
        return $"Basic {encodedCredentials}";
    }
}