using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Refit;
using Signify.A1C.Svc.Core.ApiClient;
using Signify.A1C.Svc.Core.ApiClient.Response;
using Signify.A1C.Svc.Core.Queries;
using Signify.A1C.Svc.Core.Tests.Utilities;
using Xunit;

namespace Signify.A1C.Svc.Core.Tests.Queries
{
    public class GetProviderInfoTest
    {
        private readonly IProviderApi _providerApi;
        private readonly GetProviderInfoHandler _getProviderInfoHandler;
        private readonly IMapper _mapper;

        public GetProviderInfoTest()
        {
            _providerApi = A.Fake<IProviderApi>();
            _mapper = A.Fake<IMapper>();
            var logger = A.Fake<ILogger<GetProviderInfoHandler>>();
            _getProviderInfoHandler = new GetProviderInfoHandler(_providerApi, logger, _mapper);
        }

        /// <summary>
        /// Response type
        /// </summary>
        /// <returns></returns>

        [Fact]
        public async Task GetProvider_ResponseType()
        {
            var providerInfo = new GetProviderInfo() { ProviderId = 42879 };
            A.CallTo(() => _providerApi.GetProviderById(A<int>._)).Returns(GetApiResponse());
            A.CallTo(() => _mapper.Map<ProviderInfoRs>(A<ProviderInfoRs>._)).Returns(GetMappedProviderResponse());
            var actualResult = await _getProviderInfoHandler.Handle(providerInfo, CancellationToken.None);
            actualResult.Should().BeOfType<ProviderInfoRs>("Provider type object");
        }

        /// <summary>
        /// Number of times called
        /// </summary>
        /// <returns></returns>

        [Fact]
        public async Task GetProvider_TimesCalled()
        {
            var providerInfo = new GetProviderInfo() { ProviderId = 42879 };
            A.CallTo(() => _providerApi.GetProviderById(A<int>._)).Returns(GetApiResponse());
            A.CallTo(() => _mapper.Map<ProviderInfoRs>(A<ProviderInfoRs>._)).Returns(GetMappedProviderResponse());
            var actualResult = await _getProviderInfoHandler.Handle(providerInfo, CancellationToken.None);
            A.CallTo(() => _providerApi.GetProviderById(A<int>._)).MustHaveHappenedOnceExactly();
        }

        /// <summary>
        /// Exception test
        /// </summary>
        /// <returns></returns>

        [Fact]
        public async Task GetProvider_ExceptionTest()
        {
            var providerInfo = new GetProviderInfo() { ProviderId = 42879 };
            A.CallTo(() => _providerApi.GetProviderById(A<int>._)).Returns(new ApiResponse<ProviderInfoRs>(new HttpResponseMessage() { StatusCode = System.Net.HttpStatusCode.BadRequest }, new ProviderInfoRs(), new RefitSettings() { ContentSerializer = new NewtonsoftJsonContentSerializer() }));
            A.CallTo(() => _mapper.Map<ProviderInfoRs>(A<ProviderInfoRs>._)).Returns(GetMappedProviderResponse());
            await Assert.ThrowsAsync<ApplicationException>(async () => await _getProviderInfoHandler.Handle(providerInfo, CancellationToken.None));
        }

        /// <summary>
        /// Null or default input
        /// </summary>
        /// <returns></returns>

        [Fact]
        public async Task GetProvider_NullOrDefaultProviderIdTest()
        {
            var providerInfo = new GetProviderInfo() { ProviderId = 0 };
            await Assert.ThrowsAsync<ApplicationException>(async () => await _getProviderInfoHandler.Handle(providerInfo, CancellationToken.None));
        }

        /// <summary>
        /// Provides the mock response
        /// </summary>
        /// <returns></returns>

        private static ApiResponse<ProviderInfoRs> GetApiResponse()
        {
            const string jsonResponse = ContentHelper.ProviderResponse; 
            var httpResponseMessage = new HttpResponseMessage()
            {
                Content = ContentHelper.GetStringContent(JsonConvert.DeserializeObject<ProviderInfoRs>(jsonResponse))
            };

            return new ApiResponse<ProviderInfoRs>(httpResponseMessage, JsonConvert.DeserializeObject<ProviderInfoRs>(jsonResponse), new RefitSettings() { ContentSerializer = new NewtonsoftJsonContentSerializer() });
        }

        /// <summary>
        /// Provides the mock response
        /// </summary>
        /// <returns></returns>
        private static ProviderInfoRs GetMappedProviderResponse()
        {
            const string jsonResponse = ContentHelper.ProviderResponse;
            return JsonConvert.DeserializeObject<ProviderInfoRs>(jsonResponse);
        }
    }
}
