using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Refit;
using Signify.uACR.Core.ApiClients.EvaluationApi;
using Signify.uACR.Core.ApiClients.EvaluationApi.Responses;
using Signify.uACR.Core.Queries;
using Xunit;

namespace Signify.uACR.Core.Tests.Queries;

public class QueryEvaluationProductCodesTests
{
    private readonly IEvaluationApi _evaluationApi = A.Fake<IEvaluationApi>();

    private QueryEvaluationProductCodesHandler CreateSubject()
        => new(A.Dummy<ILogger<QueryEvaluationProductCodesHandler>>(), _evaluationApi);

    [Fact]
    public async Task Handle_WithRequest_QueryEvaluationProductCodesForEvaluationId()
    {
        const long evaluationId = 1;

        var request = new QueryEvaluationProductCodes(evaluationId);
        
        var evaluationProductCodes = new EvaluationProductCodes();
        evaluationProductCodes.ProductCodes.Add("UACR");
        evaluationProductCodes.ProductCodes.Add("EGFR");
            
        var apiResponse = new ApiResponse<EvaluationProductCodes>(new HttpResponseMessage(HttpStatusCode.OK),
            evaluationProductCodes, null!);
        A.CallTo(() => _evaluationApi.GetEvaluationProductCodes(A<long>._)).Returns(apiResponse);
        
        var subject = CreateSubject();

        var result = await subject.Handle(request, CancellationToken.None);

        A.CallTo(() => _evaluationApi.GetEvaluationProductCodes(A<long>.That.Matches(id => id == evaluationId)))
            .MustHaveHappenedOnceExactly();

        Assert.Equal(apiResponse.Content.ProductCodes, result);
    }
}