using FakeItEasy;
using FluentAssertions;
using Newtonsoft.Json;
using Signify.HBA1CPOC.Svc.Core.ApiClient.Response;
using Signify.HBA1CPOC.Svc.Core.ApiClient;
using Signify.HBA1CPOC.Svc.Core.Constants.Questions.Performed;
using Signify.HBA1CPOC.Svc.Core.Constants.Questions;
using Signify.HBA1CPOC.Svc.Core.Models;
using Signify.HBA1CPOC.Svc.Core.Queries;
using Signify.HBA1CPOC.Svc.Core.Tests.Mocks.Json.Queries;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.HBA1CPOC.Svc.Core.Tests.Queries;

public class CheckHBA1CPOCEvalTest
{
    private readonly IEvaluationApi _evalApi;
    private readonly CheckHBA1CPOCEvalHandler _checkHBA1CPOCEvalHandler;
    public CheckHBA1CPOCEvalTest()
    {
        _evalApi = A.Fake<IEvaluationApi>();
        _checkHBA1CPOCEvalHandler = new CheckHBA1CPOCEvalHandler(_evalApi);
    }

    /// <summary>
    /// Response type
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GetEval_ResponseType()
    {
        var evalInfo = new CheckHBA1CPOCEval { EvaluationId = 323084 };
        A.CallTo(() => _evalApi.GetEvaluationVersion(A<int>._, A<string>._)).Returns(GetApiResponse());
        var actualResult = await _checkHBA1CPOCEvalHandler.Handle(evalInfo, CancellationToken.None);
        actualResult.Should().BeOfType<EvaluationAnswers>("EvaluationAnswers type object");
    }

    /// <summary>
    /// Response type
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GetEval_DataCheck()
    {
        var evalInfo = new CheckHBA1CPOCEval { EvaluationId = 323084 };
        A.CallTo(() => _evalApi.GetEvaluationVersion(A<int>._, A<string>._)).Returns(GetApiResponse());
        var actualResult = await _checkHBA1CPOCEvalHandler.Handle(evalInfo, CancellationToken.None);
        actualResult.IsHBA1CEvaluation.Should().BeTrue("evaluation type object");
    }
    /// <summary>
    /// Number of times called
    /// </summary>
    /// <returns></returns>

    [Fact]
    public async Task GetEval_TimesCalled()
    {
        var evalInfo = new CheckHBA1CPOCEval { EvaluationId = 323084 };
        A.CallTo(() => _evalApi.GetEvaluationVersion(A<int>._, A<string>._)).Returns(GetApiResponse());
        await _checkHBA1CPOCEvalHandler.Handle(evalInfo, CancellationToken.None);
        A.CallTo(() => _evalApi.GetEvaluationVersion(A<int>._, A<string>._)).MustHaveHappenedOnceExactly();
    }

    /// <summary>
    /// Null or default input
    /// </summary>
    /// <returns></returns>
    [Theory]
    [InlineData(PercentA1CQuestion.AnswerId)]
    [InlineData(ExpirationDateQuestion.AnswerId)]
    [InlineData(HbA1CPocTestPerformedQuestion.YesAnswerId)]
    public async Task GetEval_WhenAnswerIdIsNotRecived(int answerId)
    {
        var evalInfo = new CheckHBA1CPOCEval { EvaluationId = 323084 };
        var apiRs = GetApiResponse();
        var itemToRemove = GetApiResponse().Evaluation.Answers.FindIndex(x => x.AnswerId == answerId);
        apiRs.Evaluation.Answers.RemoveAt(itemToRemove);
        A.CallTo(() => _evalApi.GetEvaluationVersion(A<int>._, A<string>._)).Returns(apiRs);
        var actualResult = await _checkHBA1CPOCEvalHandler.Handle(evalInfo, CancellationToken.None);
        actualResult.IsHBA1CEvaluation.Should().BeFalse("An answer Id is missing");
    }

    /// <summary>
    /// Null or default input
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GetEval_NullOrDefaultProviderIdTest()
    {
        var evalInfo = new CheckHBA1CPOCEval { EvaluationId = 323084 };
        A.CallTo(() => _evalApi.GetEvaluationVersion(A<int>._, A<string>._)).Returns(new EvaluationVersionRs());
        var actualResult = await _checkHBA1CPOCEvalHandler.Handle(evalInfo, CancellationToken.None);
        actualResult.A1CPercent.Should().BeNull();
    }

    /// <summary>
    /// Provides the mock response
    /// </summary>
    /// <returns></returns>
    private static EvaluationVersionRs GetApiResponse()
    {
        return JsonConvert.DeserializeObject<EvaluationVersionRs>(QueriesAPIResponse.EvaluationVersion);
    }
}