using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
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

public class GetMemberInfoTest
{
    private readonly IMemberInfoApi _memberApi;
    private readonly GetMemberInfoHandler _getMemberInfoHandler;
    private readonly IMapper _mapper;

    public GetMemberInfoTest()
    {
        _memberApi = A.Fake<IMemberInfoApi>();
        _mapper = A.Fake<IMapper>();
        var logger = A.Fake<ILogger<GetMemberInfoHandler>>();
        _getMemberInfoHandler = new GetMemberInfoHandler(logger, _memberApi, _mapper);
    }

    /// <summary>
    /// Response type
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GetMember_ResponseType()
    {
        var providerInfo = new GetMemberInfo { MemberPlanId = 21074095 };
        A.CallTo(() => _memberApi.GetMemberInfoById(A<int>._)).Returns(GetApiResponse());
        A.CallTo(() => _mapper.Map<MemberInfoRs>(A<MemberInfoRs>._)).Returns(GetMappedMemberResponse());
        var actualResult = await _getMemberInfoHandler.Handle(providerInfo, CancellationToken.None);
        actualResult.Should().BeOfType<MemberInfoRs>("Member type object");
    }

    /// <summary>
    /// Number of times called
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GetMember_TimesCalled()
    {
        var providerInfo = new GetMemberInfo { MemberPlanId = 21074095 };
        A.CallTo(() => _memberApi.GetMemberInfoById(A<int>._)).Returns(GetApiResponse());
        A.CallTo(() => _mapper.Map<MemberInfoRs>(A<MemberInfoRs>._)).Returns(GetMappedMemberResponse());
        await _getMemberInfoHandler.Handle(providerInfo, CancellationToken.None);
        A.CallTo(() => _memberApi.GetMemberInfoById(A<int>._)).MustHaveHappenedOnceExactly();
    }

    /// <summary>
    /// Exception test
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GetMember_ExceptionTest()
    {
        var providerInfo = new GetMemberInfo { MemberPlanId = 21074095 };
        A.CallTo(() => _memberApi.GetMemberInfoById(A<int>._)).Returns(new ApiResponse<MemberInfoRs>(new HttpResponseMessage() { StatusCode = System.Net.HttpStatusCode.BadRequest }, new MemberInfoRs(), new RefitSettings()));
        A.CallTo(() => _mapper.Map<MemberInfoRs>(A<MemberInfoRs>._)).Returns(GetMappedMemberResponse());
        await Assert.ThrowsAsync<ApplicationException>(async () => await _getMemberInfoHandler.Handle(providerInfo, CancellationToken.None));
    }

    /// <summary>
    /// Null or default input
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GetMember_NullOrDefaultProviderIdTest()
    {
        var providerInfo = new GetMemberInfo { MemberPlanId = 0 };
        await Assert.ThrowsAsync<ApplicationException>(async () => await _getMemberInfoHandler.Handle(providerInfo, CancellationToken.None));
    }

    /// <summary>
    /// Provides the mock response
    /// </summary>
    /// <returns></returns>
    private static ApiResponse<MemberInfoRs> GetApiResponse()
    {
        var jsonResponse = QueriesAPIResponse.Member;
        var httpResponseMessage = new HttpResponseMessage
        {
            Content = ContentHelper.GetStringContent(JsonConvert.DeserializeObject<MemberInfoRs>(jsonResponse))
        };

        return new ApiResponse<MemberInfoRs>(httpResponseMessage, JsonConvert.DeserializeObject<MemberInfoRs>(jsonResponse), new RefitSettings());
    }

    /// <summary>
    /// Provides the mock response
    /// </summary>
    /// <returns></returns>
    private static MemberInfoRs GetMappedMemberResponse()
    {
        return JsonConvert.DeserializeObject<MemberInfoRs>(QueriesAPIResponse.Member);
    }
}