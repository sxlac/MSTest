using FakeItEasy;
using FluentAssertions;
using Newtonsoft.Json;
using Refit;
using Signify.HBA1CPOC.Svc.Core.ApiClient.Response;
using Signify.HBA1CPOC.Svc.Core.ApiClient;
using Signify.HBA1CPOC.Svc.Core.Queries;
using Signify.HBA1CPOC.Svc.Core.Tests.Mocks.Json.Queries;
using Signify.HBA1CPOC.Svc.Core.Tests.Utilities;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.HBA1CPOC.Svc.Core.Tests.Queries;

public class GetProviderInfoTest
{
    private readonly IProviderApi _providerApi;
    private readonly GetProviderInfoHandler _getProviderInfoHandler;
    public GetProviderInfoTest()
    {
        _providerApi = A.Fake<IProviderApi>();
        _getProviderInfoHandler = new GetProviderInfoHandler(_providerApi);
    }
    /// <summary>
    /// Response type
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GetProvider_ResponseType()
    {
        var providerInfo = new GetProviderInfo { ProviderId = 42879 };
        A.CallTo(() => _providerApi.GetProviderById(A<int>._)).Returns(GetApiResponse());
        var actualResult = await _getProviderInfoHandler.Handle(providerInfo, CancellationToken.None);
        actualResult.Should().BeOfType<ProviderRs>("Provider type object");
    }

    /// <summary>
    /// Number of times called
    /// </summary>
    /// <returns></returns>
        
    [Fact]
    public async Task GetProvider_TimesCalled()
    {
        var providerInfo = new GetProviderInfo { ProviderId = 42879 };
        A.CallTo(() => _providerApi.GetProviderById(A<int>._)).Returns(GetApiResponse());
        await _getProviderInfoHandler.Handle(providerInfo, CancellationToken.None);
        A.CallTo(() => _providerApi.GetProviderById(A<int>._)).MustHaveHappenedOnceExactly();
    }

    /// <summary>
    /// Exception test
    /// </summary>
    /// <returns></returns>
        
    [Fact]
    public async Task GetProvider_ExceptionTest()
    {
        var providerInfo = new GetProviderInfo { ProviderId = 42879 };
        A.CallTo(() => _providerApi.GetProviderById(A<int>._)).Returns(new ApiResponse<ProviderRs>(new HttpResponseMessage() { StatusCode = System.Net.HttpStatusCode.BadRequest},new ProviderRs(), new RefitSettings()));
        await Assert.ThrowsAsync<ApplicationException>(async () =>await _getProviderInfoHandler.Handle(providerInfo, CancellationToken.None));
    }

    /// <summary>
    /// Null or default input
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GetProvider_NullOrDefaultProviderIdTest()
    {
        var providerInfo = new GetProviderInfo { ProviderId = 0 };
        A.CallTo(() => _providerApi.GetProviderById(A<int>._)).Returns(new ApiResponse<ProviderRs>(new HttpResponseMessage() { StatusCode = System.Net.HttpStatusCode.BadRequest }, new ProviderRs(), new RefitSettings()));
        var actualResult = await _getProviderInfoHandler.Handle(providerInfo, CancellationToken.None);
        actualResult.Should().BeNull();
    }

    /// <summary>
    /// Provides the mock response
    /// </summary>
    /// <returns></returns>
    private static ApiResponse<ProviderRs> GetApiResponse()
    {
        var httpResponseMessage = new HttpResponseMessage
        {
            Content = ContentHelper.GetStringContent(JsonConvert.DeserializeObject<ProviderRs>(QueriesAPIResponse.ProviderById))
        };

        return new ApiResponse<ProviderRs>(httpResponseMessage, JsonConvert.DeserializeObject<ProviderRs>(QueriesAPIResponse.ProviderById), new RefitSettings());
    }
}