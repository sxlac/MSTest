using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Refit;
using Signify.CKD.Svc.Core.ApiClient;
using Signify.CKD.Svc.Core.ApiClient.Response;
using Signify.CKD.Svc.Core.Configs;
using Signify.CKD.Svc.Core.Infrastructure;
using Signify.CKD.Svc.Core.Tests.Mocks.Json.Queries;
using Signify.CKD.Svc.Core.Tests.Utilities;
using Xunit;

namespace Signify.CKD.Svc.Core.Tests.Infrastructure
{
    public class OktaClientHandlerTests : IClassFixture<EntityFixtures>
    {
        private readonly OktaClientCredentialsHttpClientHandler _handler;
        private readonly EntityFixtures _entityFixtures;
        private readonly IOktaApi _oktaApi;
        private readonly ILogger<OktaClientCredentialsHttpClientHandler> _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly OktaConfig _oktaSettings;



        public OktaClientHandlerTests(EntityFixtures entityFixtures)
        {
            _logger = A.Fake<ILogger<OktaClientCredentialsHttpClientHandler>>();
            _entityFixtures = entityFixtures;
            _oktaApi = A.Fake<IOktaApi>();
            _oktaSettings = A.Fake<OktaConfig>();
            _memoryCache = A.Fake<IMemoryCache>();
            _handler = new OktaClientCredentialsHttpClientHandler(_oktaSettings, _memoryCache, _oktaApi, _logger)
            {
                InnerHandler = new TestHandler()
            };
        }


        [Fact]
        public async Task Should_Call_SendAsync_Return_OK()
        {
            string accToken = "12345";
            var jsonResponse = QueriesAPIResponse.ACCESSTOKEN;
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
            string accToken = "12345";
            var jsonResponse = QueriesAPIResponse.ACCESSTOKEN;
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
        public class TestHandler : DelegatingHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.Factory.StartNew(() => new HttpResponseMessage(HttpStatusCode.OK), cancellationToken);
            }
        }
    }
}
