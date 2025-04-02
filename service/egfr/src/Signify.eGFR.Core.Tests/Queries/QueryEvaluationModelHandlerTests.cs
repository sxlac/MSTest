using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.eGFR.Core.ApiClients.EvaluationApi;
using Signify.eGFR.Core.ApiClients.EvaluationApi.Responses;
using Signify.eGFR.Core.Queries;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.eGFR.Core.Tests.Queries;

public class QueryEvaluationModelHandlerTests
{
    private readonly IEvaluationApi _evaluationApi = A.Fake<IEvaluationApi>();

    private QueryEvaluationModelHandler CreateSubject()
        => new(A.Dummy<ILogger<QueryEvaluationModelHandler>>(), _evaluationApi);

    [Fact]
    public async Task Handle_WithRequest_QueriesEvaluationApiForEvaluationId()
    {
        const int evaluationId = 1;

        var request = new QueryEvaluationModel(evaluationId);
        var version = new EvaluationVersion
        {
            Evaluation = new EvaluationModel()
        };

        A.CallTo(() => _evaluationApi.GetEvaluationVersion(A<long>._, A<string>._))
            .Returns(Task.FromResult(version));

        var subject = CreateSubject();

        var result = await subject.Handle(request, CancellationToken.None);

        A.CallTo(() => _evaluationApi.GetEvaluationVersion(A<long>.That.Matches(id => id == evaluationId), A<string>._))
            .MustHaveHappenedOnceExactly();

        Assert.Equal(version.Evaluation, result);
    }
}