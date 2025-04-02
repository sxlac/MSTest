using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Refit;
using Signify.HBA1CPOC.Svc.Core.ApiClient.Response;
using Signify.HBA1CPOC.Svc.Core.ApiClient;
using Signify.HBA1CPOC.Svc.Core.Configs;
using Signify.HBA1CPOC.Svc.Core.Infrastructure;
using Signify.HBA1CPOC.Svc.Core.Tests.Mocks.Json.Queries;
using Signify.HBA1CPOC.Svc.Core.Tests.Utilities;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.HBA1CPOC.Svc.Core.Tests.Infrastructure;

public class OktaClientHandlerTests
{
    private readonly OktaClientCredentialsHttpClientHandler _handler;
    private readonly IOktaApi _oktaApi;

    public OktaClientHandlerTests()
    {
        var logger = A.Fake<ILogger<OktaClientCredentialsHttpClientHandler>>();
        _oktaApi = A.Fake<IOktaApi>();
        var oktaSettings = A.Fake<OktaConfig>();
        var memoryCache = A.Fake<IMemoryCache>();
        _handler = new OktaClientCredentialsHttpClientHandler(oktaSettings, memoryCache, _oktaApi, logger)
        {
            InnerHandler = new TestHandler()
        };
    }


    [Fact]
    public async Task Should_Call_SendAsync_Return_OK()
    {
        const string accToken = "12345";
        const string jsonResponse = QueriesAPIResponse.AccessToken;
        var httpResponseMessage = new HttpResponseMessage()
        {
            Content = ContentHelper.GetStringContent(JsonConvert.DeserializeObject<AccessToken>(jsonResponse))
        };
        var token = new ApiResponse<AccessToken>(httpResponseMessage, JsonConvert.DeserializeObject<AccessToken>(jsonResponse), new RefitSettings());
        A.CallTo(() => _oktaApi.GetAccessToken(A<string>._, A<string>._, A<string>._)).Returns(token);



        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://www.google.com/"); //some url for making request
        httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", accToken); //Value of auth header does not really matter even if we are not using Bearer token
        var invoker = new HttpMessageInvoker(_handler);



        //Action
        var result = await invoker.SendAsync(httpRequestMessage, new CancellationToken());



        // Assert
        result.Headers.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }
    [Fact]
    public async Task Should_Call_GetAccessToken_MustHappen_Once()
    {
        const string accToken = "12345";
        const string jsonResponse = QueriesAPIResponse.AccessToken;
        var httpResponseMessage = new HttpResponseMessage()
        {
            Content = ContentHelper.GetStringContent(JsonConvert.DeserializeObject<AccessToken>(jsonResponse))
        };
        var token = new ApiResponse<AccessToken>(httpResponseMessage, JsonConvert.DeserializeObject<AccessToken>(jsonResponse), new RefitSettings());
        A.CallTo(() => _oktaApi.GetAccessToken(A<string>._, A<string>._, A<string>._)).Returns(token);



        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://www.google.com/"); //some url for making request
        httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", accToken); //Value of auth header does not really matter even if we are not using Bearer token
        var invoker = new HttpMessageInvoker(_handler);



        //Action
        var result = await invoker.SendAsync(httpRequestMessage, new CancellationToken());



        // Assert
        A.CallTo(() => _oktaApi.GetAccessToken(A<string>._, A<string>._, A<string>._))
            .MustHaveHappenedOnceExactly();
        result.Headers.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }


    //This class is used for inner handler set up, without which the base call would fail
    private class TestHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(() => new HttpResponseMessage(HttpStatusCode.OK), cancellationToken);
        }
    }
}