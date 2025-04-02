using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Polly;
using Signify.CKD.Svc.Core.ApiClient;
using Signify.CKD.Svc.Core.ApiClient.Requests;
using Signify.CKD.Svc.Core.ApiClient.Response;
using Signify.CKD.Svc.Core.Configs;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Policy = Polly.Policy;

namespace Signify.CKD.Svc.Core.Infrastructure
{

		public class OktaClientCredentialsHttpClientHandler : DelegatingHandler
		{
			private const string Key = "ClientAccessToken";
			private readonly IMemoryCache _memoryCache;
			private readonly IOktaApi _oktaApi;
			private readonly OktaConfig _oktaSettings;
			private readonly ILogger<OktaClientCredentialsHttpClientHandler> _logger;

			public OktaClientCredentialsHttpClientHandler(OktaConfig oktaConfig, IMemoryCache memoryCache, IOktaApi oktaApi, ILogger<OktaClientCredentialsHttpClientHandler> logger)
			{
				_oktaSettings = oktaConfig;
				_memoryCache = memoryCache;
				_oktaApi = oktaApi;
				_logger = logger;
			}

			protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
				CancellationToken cancellationToken)
			{
				var auth = request.Headers.Authorization;
				var token = await GetDeviceToken();
				request.Headers.Authorization = new AuthenticationHeaderValue(auth.Scheme, token.access_token);

				var p = Policy.HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.Unauthorized)
					.Retry(1, (e, r) =>
					{
						_memoryCache.Remove(Key);
						token = GetDeviceToken().Result;
						request.Headers.Authorization = new AuthenticationHeaderValue(auth.Scheme, token.access_token);
					});
				var result = p.Execute(() => base.SendAsync(request, cancellationToken).Result);
				return result;
			}

			private async Task<AccessToken> GetDeviceToken()
			{
				//Local Function!!
				async Task<AccessToken> GetToken(ICacheEntry cacheEntry)
				{
					string Base64Encode(string username, string password)
					{
						var auth = $"{username}:{password}";
						var plainTextBytes = Encoding.UTF8.GetBytes(auth);
						var encodedCredentials = Convert.ToBase64String(plainTextBytes);
						return $"Basic {encodedCredentials}";
					}

					var authorizationHeader = Base64Encode(_oktaSettings.ClientId, _oktaSettings.ClientSecret);
					var request = new OktaTokenRequest
					{
						grant_type = "client_credentials",
						scope = _oktaSettings.Scopes
					};
					var response = await _oktaApi.GetAccessToken(authorizationHeader, "application/x-www-form-urlencoded", request.ToString());
					if (response.IsSuccessStatusCode)
					{
						return response.Content;
					}
					else
					{
						//do we need to throw exception?  Probably, so the wrapping command can have its own retry policy take over.
						_logger.LogError($"Unable to authenticate Okta access token: {response}", response);
						throw response.Error;
					}
				}

				var accessToken = await _memoryCache.GetOrCreateAsync(Key, GetToken);

				return accessToken;
			}
		}
}
