using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using Newtonsoft.Json;
using Signify.A1C.Svc.Core.ApiClient;
using Signify.A1C.Svc.Core.ApiClient.Response;
using Signify.A1C.Svc.Core.Queries;
using Xunit;
using Signify.A1C.Svc.Core.Tests.Mocks.Json.Queries;

namespace Signify.A1C.Svc.Core.Tests.Queries
{
    public class CheckA1CEvalTest
    {
        private readonly IEvaluationApi _evalApi;
        private readonly CheckA1CEvalHandler _checkA1CEvalHandler;
        public CheckA1CEvalTest()
        {
            _evalApi = A.Fake<IEvaluationApi>();
            _checkA1CEvalHandler = new CheckA1CEvalHandler(_evalApi);
        }

        /// <summary>
        /// Response type
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Should_Get_Text_ResponseType()
        {
            var evalInfo = new CheckA1CEval() { EvaluationId = 323084 };
            A.CallTo(() => _evalApi.GetEvaluationVersion(A<int>._, A<string>._)).Returns(GetApiResponse());
            var actualResult = await _checkA1CEvalHandler.Handle(evalInfo, CancellationToken.None);
            actualResult.Should().BeOfType<string>("EvaluationAnswer is text value");
        }

        /// <summary>
        /// Response type
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task GetEval_DataCheck()
        {
            var evalInfo = new CheckA1CEval() { EvaluationId = 323084 };
            A.CallTo(() => _evalApi.GetEvaluationVersion(A<int>._, A<string>._)).Returns(GetApiResponse());
            var actualResult = await _checkA1CEvalHandler.Handle(evalInfo, CancellationToken.None);
            actualResult.Length.Should().BeGreaterThan(0);
        }
        /// <summary>
        /// Number of times called
        /// </summary>
        /// <returns></returns>

        [Fact]
        public async Task Should_Call_EvalApi_Once()
        {
            var evalInfo = new CheckA1CEval() { EvaluationId = 323084 };
            A.CallTo(() => _evalApi.GetEvaluationVersion(A<int>._, A<string>._)).Returns(GetApiResponse());
            await _checkA1CEvalHandler.Handle(evalInfo, CancellationToken.None);
            A.CallTo(() => _evalApi.GetEvaluationVersion(A<int>._, A<string>._)).MustHaveHappenedOnceExactly();
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
}
