﻿using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Polly;
using Signify.DEE.Svc.Core.ApiClient;
using Signify.DEE.Svc.Core.ApiClient.Requests;
//using Signify.DEE.Svc.Core.ApiClient.Response;
using Signify.DEE.Svc.Core.Configs;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Policy = Polly.Policy;

namespace Signify.DEE.Svc.Core.Infrastructure;

[ExcludeFromCodeCoverage]
public class OktaClientCredentialsHttpClientHandler(
	OktaConfig oktaConfig,
	IMemoryCache memoryCache,
	IOktaApi oktaApi,
	ILogger<OktaClientCredentialsHttpClientHandler> logger)
	: DelegatingHandler
{
	private const string Key = "ClientAccessToken";

	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		var auth = request.Headers.Authorization;
		var token = await GetDeviceToken();
		request.Headers.Authorization = new AuthenticationHeaderValue(auth.Scheme, token.access_token);

		var p = Policy.HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.Unauthorized)
			.Retry(1, (e, r) =>
			{
				memoryCache.Remove(Key);
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

			var authorizationHeader = Base64Encode(oktaConfig.ClientId, oktaConfig.ClientSecret);
			var request = new OktaTokenRequest
			{
				grant_type = "client_credentials",
				scope = oktaConfig.Scopes.ToString()
			};
			var response = await oktaApi.GetAccessToken(authorizationHeader, "application/x-www-form-urlencoded", request.ToString());
			if (response.IsSuccessStatusCode)
			{
				return response.Content;
			}
			else
			{
				//do we need to throw exception?  Probably, so the wrapping command can have its own retry policy take over.
				logger.LogError("Unable to authenticate Okta access token: {response}", response);
				throw response.Error;
			}
		}

		var accessToken = await memoryCache.GetOrCreateAsync(Key, GetToken);

		return accessToken;
	}
}