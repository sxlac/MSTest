using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Polly;
using Signify.uACR.Core.ApiClients.OktaApi;
using Signify.uACR.Core.ApiClients.OktaApi.Requests;
using Signify.uACR.Core.ApiClients.OktaApi.Responses;
using Signify.uACR.Core.Configs;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.uACR.Core.ApiClients;

[ExcludeFromCodeCoverage]
public class OAuthClientCredentialsHttpClientHandler : DelegatingHandler
{
    private const string Key = "ClientAccessToken";

    private readonly ILogger _logger;

    private readonly IMemoryCache _memoryCache;

    private readonly IOktaApi _oktaApi;
    private readonly OktaConfig _oktaConfig;

    public OAuthClientCredentialsHttpClientHandler(ILogger<OAuthClientCredentialsHttpClientHandler> logger,
        IMemoryCache memoryCache,
        IOktaApi oktaApi,
        OktaConfig oktaConfig)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _oktaApi = oktaApi ?? throw new ArgumentNullException(nameof(oktaApi));
        _oktaConfig = oktaConfig ?? throw new ArgumentNullException(nameof(oktaConfig));
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        async Task<AuthenticationHeaderValue> GetAuthenticationHeaderValue()
        {
            var authnHeader = request.Headers.Authorization;
            var token = await GetDeviceToken();
            return new AuthenticationHeaderValue(authnHeader?.Scheme ?? "Bearer", token.access_token);
        }

        request.Headers.Authorization = await GetAuthenticationHeaderValue();

        return await Policy
            .HandleResult<HttpResponseMessage>(response => response.StatusCode == HttpStatusCode.Unauthorized)
            .RetryAsync(1, async (_, _) =>
            {
                _memoryCache.Remove(Key);
                request.Headers.Authorization = await GetAuthenticationHeaderValue();
            })
            .ExecuteAsync(() => base.SendAsync(request, cancellationToken));
    }

    private async Task<AccessTokenResponse> GetDeviceToken()
    {
        async Task<AccessTokenResponse> GetToken(ICacheEntry cacheEntry)
        {
            string Base64Encode(string username, string password)
            {
                var auth = $"{username}:{password}";
                var plainTextBytes = Encoding.UTF8.GetBytes(auth);
                var encodedCredentials = Convert.ToBase64String(plainTextBytes);
                return $"Basic {encodedCredentials}";
            }

            var authorizationHeader = Base64Encode(_oktaConfig.ClientId, _oktaConfig.ClientSecret);

            var request = new OktaTokenRequest
            {
                scope = string.Join(' ', _oktaConfig.Scopes)
            };

            var response = await _oktaApi.GetAccessToken(authorizationHeader, "application/x-www-form-urlencoded", request.ToString());
            if (response.IsSuccessStatusCode)
                return response.Content;

            //do we need to throw exception?  Probably, so the wrapping command can have its own retry policy take over.
            _logger.LogError("Unable to authenticate Okta access token. StatusCode={StatusCode}, Reason={Reason}, Request={Request}",
                response.StatusCode, response.ReasonPhrase, request);

            if (response.Error != null)
                throw response.Error;
            throw new HttpRequestException("Failed to obtain access token from Okta");
        }

        return await _memoryCache.GetOrCreateAsync(Key, GetToken);
    }
}