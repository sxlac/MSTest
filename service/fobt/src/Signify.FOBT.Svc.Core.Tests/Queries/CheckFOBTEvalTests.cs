using FakeItEasy;
using FluentAssertions;
using Newtonsoft.Json;
using Signify.FOBT.Svc.Core.ApiClient.Response;
using Signify.FOBT.Svc.Core.ApiClient;
using Signify.FOBT.Svc.Core.Queries;
using Signify.FOBT.Svc.Core.Tests.Mocks.Json.Queries;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.Queries;

public class CheckFOBTEvalTest
{
    private readonly IEvaluationApi _evalApi;
    private readonly CheckFOBTEvalHandler _handler;
    public CheckFOBTEvalTest()
    {
        _evalApi = A.Fake<IEvaluationApi>();
        _handler = new CheckFOBTEvalHandler(_evalApi);
    }

    /// <summary>
    /// Response type
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GetEval_ResponseType()
    {
        var evalInfo = new CheckFOBTEval { EvaluationId = 323084 };
        A.CallTo(() => _evalApi.GetEvaluationVersion(A<long>._, A<string>._)).Returns(GetApiResponse());
        var actualResult = await _handler.Handle(evalInfo, CancellationToken.None);
        actualResult.Should().BeOfType<string>("231455");
    }

    /// <summary>
    /// Response type
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GetEval_DataCheck()
    {
        var evalInfo = new CheckFOBTEval {EvaluationId = 323084};
        A.CallTo(() => _evalApi.GetEvaluationVersion(A<long>._, A<string>._)).Returns(GetApiResponse());
        var actualResult = await _handler.Handle(evalInfo, CancellationToken.None);
        actualResult.Should().NotBeEmpty();
    }

    /// <summary>
    /// Number of times called
    /// </summary>
    /// <returns></returns>

    [Fact]
    public async Task GetEval_TimesCalled()
    {
        var evalInfo = new CheckFOBTEval { EvaluationId = 323084 };
        A.CallTo(() => _evalApi.GetEvaluationVersion(A<long>._, A<string>._)).Returns(GetApiResponse());
        await _handler.Handle(evalInfo, CancellationToken.None);
        A.CallTo(() => _evalApi.GetEvaluationVersion(A<long>._, A<string>._)).MustHaveHappenedOnceExactly();
    }

       
    /// <summary>
    /// Null or default input
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GetEval_NullOrDefaultProviderIdTest()
    {
        var evalInfo = new CheckFOBTEval { EvaluationId = 323084 };
        A.CallTo(() => _evalApi.GetEvaluationVersion(A<long>._, A<string>._)).Returns(new EvaluationVersionRs());
        var actualResult = await _handler.Handle(evalInfo, CancellationToken.None);
        actualResult.Should().BeNullOrEmpty();
    }

    /// <summary>
    /// Provides the mock response
    /// </summary>
    /// <returns></returns>
    private static EvaluationVersionRs GetApiResponse()
    {
        return JsonConvert.DeserializeObject<EvaluationVersionRs>(QueriesAPIResponse.EVALUATIONVERSION);
    }
}