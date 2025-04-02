using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.Spirometry.Core.ApiClients.EvaluationApi.Responses;
using Signify.Spirometry.Core.ApiClients.EvaluationApi;
using Signify.Spirometry.Core.Queries;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Queries;

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

        A.CallTo(() => _evaluationApi.GetEvaluationVersion(A<int>._, A<string>._))
            .Returns(Task.FromResult(version));

        var subject = CreateSubject();

        var result = await subject.Handle(request, CancellationToken.None);

        A.CallTo(() => _evaluationApi.GetEvaluationVersion(A<int>.That.Matches(id => id == evaluationId), A<string>._))
            .MustHaveHappenedOnceExactly();

        Assert.Equal(version.Evaluation, result);
    }
}