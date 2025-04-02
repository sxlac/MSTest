using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Refit;
using Signify.FOBT.Svc.Core.ApiClient.Response;
using Signify.FOBT.Svc.Core.ApiClient;
using Signify.FOBT.Svc.Core.Queries;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.Queries;

public class GetProviderInfoTest
{
    private readonly IProviderApi _providerApi;
    private readonly GetProviderInfoHandler _handler;

    public GetProviderInfoTest()
    {
        var mapper = A.Fake<IMapper>();
        var logger = A.Fake<ILogger<GetProviderInfoHandler>>();
        _providerApi = A.Fake<IProviderApi>();
        _handler = new GetProviderInfoHandler(_providerApi, logger, mapper);
    }


    /// <summary>
    /// Number of times called
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GetProvider_TimesCalled()
    {
        var providerInfo = new GetProviderInfo { ProviderId = 42879 };
        A.CallTo(() => _providerApi.GetProviderById(A<int>._)).Returns(
            new ApiResponse<ProviderInfoRs>(new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.OK },
                new ProviderInfoRs(),
                new RefitSettings()));
        await _handler.Handle(providerInfo, CancellationToken.None);
        A.CallTo(() => _providerApi.GetProviderById(A<int>._)).MustHaveHappenedOnceExactly();
    }

    /// <summary>
    /// Exception test
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GetProvider_ExceptionTest()
    {
        var providerInfo = new GetProviderInfo { ProviderId = 0 };
        A.CallTo(() => _providerApi.GetProviderById(A<int>._)).Returns(new ApiResponse<ProviderInfoRs>(
            new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.BadRequest },
            new ProviderInfoRs(),
            new RefitSettings()));
        await Assert.ThrowsAsync<ApplicationException>(async () =>
            await _handler.Handle(providerInfo, CancellationToken.None));
    }

    /// <summary>
    /// Null or default input
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GetProvider_DefaultProviderIdTest()
    {
        var providerInfo = new GetProviderInfo { ProviderId = 42879 };
        A.CallTo(() => _providerApi.GetProviderById(A<int>._)).Returns(new ApiResponse<ProviderInfoRs>(
            new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.OK }, new ProviderInfoRs(),
            new RefitSettings()));
        var actualResult = await _handler.Handle(providerInfo, CancellationToken.None);
        actualResult.Should().NotBeNull();
    }
}