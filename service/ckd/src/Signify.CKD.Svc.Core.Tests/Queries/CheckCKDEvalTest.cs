using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Signify.CKD.Svc.Core.ApiClient;
using Signify.CKD.Svc.Core.ApiClient.Response;
using Signify.CKD.Svc.Core.Data.Entities;
using Signify.CKD.Svc.Core.Models;
using Signify.CKD.Svc.Core.Queries;
using Signify.CKD.Svc.Core.Tests.Mocks.Json.Queries;
using Signify.CKD.Svc.Core.Tests.Utilities;
using System;
using System.Collections.Generic;
using Signify.CKD.Svc.Core.Infrastructure.Observability;
using LookUp = Signify.CKD.Svc.Core.Queries.Helpers.Lookup;
using Xunit;

namespace Signify.CKD.Svc.Core.Tests.Queries
{
    public sealed class CheckCKDEvalTest : IAsyncDisposable, IDisposable
    {
        private readonly IEvaluationApi _evalApi;
        private readonly CheckCKDEvalHandler _checkCKDEvalHandler;
        private readonly MockDbFixture _fixture = new MockDbFixture();
        private readonly IObservabilityService _observabilityService = A.Fake<IObservabilityService>();
        
        public CheckCKDEvalTest()
        {
            _evalApi = A.Fake<IEvaluationApi>();
            _checkCKDEvalHandler = new CheckCKDEvalHandler(A.Dummy<ILogger<CheckCKDEvalHandler>>(), _evalApi, _fixture.Context, _observabilityService);
        }

        public ValueTask DisposeAsync()
        {
            return _fixture.DisposeAsync();
        }

        public void Dispose()
        {
            _fixture.Dispose();
        }

        /// <summary>
        /// Response type
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task GetEval_ResponseType()
        {
            var evalInfo = new CheckCKDEval() { EvaluationId = 323084 };
            A.CallTo(() => _evalApi.GetEvaluationVersion(A<int>._, A<string>._)).Returns(GetApiResponse());
            var actualResult = await _checkCKDEvalHandler.Handle(evalInfo, CancellationToken.None);
            actualResult.Should().BeOfType<EvaluationAnswers>("EvaluationAnswers type object");
        }

        /// <summary>
        /// Response type
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task GetEval_DataCheck()
        {
            var evalInfo = new CheckCKDEval() { EvaluationId = 323084, };
            var evalResponse = GetApiResponse();
            A.CallTo(() => _evalApi.GetEvaluationVersion(A<int>._, A<string>._)).Returns(evalResponse);
            var actualResult = await _checkCKDEvalHandler.Handle(evalInfo, CancellationToken.None);
            actualResult.IsCKDEvaluation.Should().BeTrue("evaluation type object");
        }

        /// <summary>
        /// Number of times called
        /// </summary>
        /// <returns></returns>

        [Fact]
        public async Task GetEval_TimesCalled()
        {
            var evalInfo = new CheckCKDEval() { EvaluationId = 323084 };
            A.CallTo(() => _evalApi.GetEvaluationVersion(A<int>._, A<string>._)).Returns(GetApiResponse());
            await _checkCKDEvalHandler.Handle(evalInfo, CancellationToken.None);
            A.CallTo(() => _evalApi.GetEvaluationVersion(A<int>._, A<string>._)).MustHaveHappenedOnceExactly();
        }

        /// <summary>
        /// Null or default input
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task GetEval_NullOrDefaultProviderIdTest()
        {
            var evalInfo = new CheckCKDEval() { EvaluationId = 323084 };
            A.CallTo(() => _evalApi.GetEvaluationVersion(A<int>._, A<string>._)).Returns(new EvaluationVersionRs());
            var actualResult = await _checkCKDEvalHandler.Handle(evalInfo, CancellationToken.None);
            actualResult.LookupCKDAnswerEntity.Should().BeNull();
        }

        /// <summary>
        /// Provides the mock response
        /// </summary>
        /// <returns></returns>
        private static EvaluationVersionRs GetApiResponse()
        {
            return JsonConvert.DeserializeObject<EvaluationVersionRs>(QueriesAPIResponse.EVALUATIONVERSION);
        }

        [Fact]
        public async Task Handle_WhenUrineNotCollected_WithReason_SetsReason()
        {
            const int answerId = 30865;//answer Id for member refused.
            const short pk = 2;

            await AddNotPerformedReasonToDb(pk, answerId);

            var answers = new List<EvaluationAnswerModel>
            {
                CreateUrineCollectedToday(false),
                CreateNotPerformedReason(answerId)
            };

            A.CallTo(() => _evalApi.GetEvaluationVersion(A<int>._, A<string>._))
                .Returns(new EvaluationVersionRs { Evaluation = new EvaluationModel { Answers = answers } });

            var actual = await _checkCKDEvalHandler.Handle(new CheckCKDEval(), default);

            A.CallTo(() => _evalApi.GetEvaluationVersion(A<int>._, A<string>._))
                .MustHaveHappened();

            Assert.False(actual.IsCKDEvaluation);
            Assert.Equal(pk, actual.NotPerformedReasonId);
            Assert.Equal(answerId, actual.NotPerformedAnswerId);
        }

        [Fact]
        public async Task Handle_WhenUrineCollectedToday_EvenIfWithNotPerformedReason_IgnoresNotPerformedReason()
        {
            const int answerId = 1;
            const short pk = 2;

            await AddNotPerformedReasonToDb(pk, answerId);

            var resultAnswer = CreateUrineMicroalbuminDipstick("answer value");

            var answers = new List<EvaluationAnswerModel>
            {
                CreateUrineCollectedToday(true),
                CreateNotPerformedReason(answerId),
                resultAnswer,
                CreateExpiryDate(DateTime.UtcNow)
            };

            await _fixture.Context.LookupCKDAnswer.AddAsync(new LookupCKDAnswer
            {
                CKDAnswerId = resultAnswer.AnswerId, // The answer id must be in the lookup table in db
                CKDAnswerValue = resultAnswer.AnswerValue
            });

            await _fixture.Context.SaveChangesAsync();

            A.CallTo(() => _evalApi.GetEvaluationVersion(A<int>._, A<string>._))
                .Returns(new EvaluationVersionRs { Evaluation = new EvaluationModel { Answers = answers } });

            var actual = await _checkCKDEvalHandler.Handle(new CheckCKDEval(), default);

            A.CallTo(() => _evalApi.GetEvaluationVersion(A<int>._, A<string>._))
                .MustHaveHappened();

            Assert.True(actual.IsCKDEvaluation);
            Assert.Null(actual.NotPerformedAnswerId);
        }


        [Fact]
        public async Task Handle_WhenDipStickAnswerInvalid_NoLookUpAnswer_ShouldStillBePerformed()
        {

            var answers = new List<EvaluationAnswerModel>
            {
                CreateUrineCollectedToday(true),
                CreateExpiryDate(DateTime.UtcNow)
            };

            A.CallTo(() => _evalApi.GetEvaluationVersion(A<int>._, A<string>._))
                .Returns(new EvaluationVersionRs { Evaluation = new EvaluationModel { Answers = answers } });

            var actual = await _checkCKDEvalHandler.Handle(new CheckCKDEval(), default);

            A.CallTo(() => _evalApi.GetEvaluationVersion(A<int>._, A<string>._))
                .MustHaveHappened();

            Assert.True(actual.IsCKDEvaluation);
            Assert.Null(actual.NotPerformedAnswerId);
        }

        [Fact]
        public async Task Handle_WhenExpiryDateInvalid_ShouldStillBePerformed()
        {

            var resultAnswer = CreateUrineMicroalbuminDipstick("answer value");

            var answers = new List<EvaluationAnswerModel>
            {
                CreateUrineCollectedToday(true),
                resultAnswer
            };

            await _fixture.Context.LookupCKDAnswer.AddAsync(new LookupCKDAnswer
            {
                CKDAnswerId = resultAnswer.AnswerId, // The answer id must be in the lookup table in db
                CKDAnswerValue = resultAnswer.AnswerValue
            });

            await _fixture.Context.SaveChangesAsync();

            A.CallTo(() => _evalApi.GetEvaluationVersion(A<int>._, A<string>._))
                .Returns(new EvaluationVersionRs { Evaluation = new EvaluationModel { Answers = answers } });

            var actual = await _checkCKDEvalHandler.Handle(new CheckCKDEval(), default);

            A.CallTo(() => _evalApi.GetEvaluationVersion(A<int>._, A<string>._))
                .MustHaveHappened();

            Assert.True(actual.IsCKDEvaluation);
            Assert.Null(actual.NotPerformedAnswerId);
        }

        [Fact]
        public async Task Handle_CkdAnswerLookUp_Returns_LookUpCkdAnswerEntity()
        {
            //Arrange
            const int answerId = 1;
            const short pk = 2;
            await AddNotPerformedReasonToDb(pk, answerId);
            var resultAnswer = CreateUrineMicroalbuminDipstick("answer value");
            var answers = new List<EvaluationAnswerModel>
            {
                CreateUrineCollectedToday(true),
                CreateNotPerformedReason(answerId),
                resultAnswer,
                CreateExpiryDate(DateTime.UtcNow)
            };
            await _fixture.Context.LookupCKDAnswer.AddAsync(new LookupCKDAnswer
            {
                CKDAnswerId = resultAnswer.AnswerId, // The answer id must be in the lookup table in db
                CKDAnswerValue = resultAnswer.AnswerValue
            });
            await _fixture.Context.SaveChangesAsync();
            A.CallTo(() => _evalApi.GetEvaluationVersion(A<int>._, A<string>._))
                .Returns(new EvaluationVersionRs { Evaluation = new EvaluationModel { Answers = answers } });

            //Act
            var actual = await _checkCKDEvalHandler.Handle(new CheckCKDEval(), default);

            //Assert
            A.CallTo(() => _evalApi.GetEvaluationVersion(A<int>._, A<string>._)).MustHaveHappened();
            actual.LookupCKDAnswerEntity.Should().BeOfType<LookupCKDAnswer>();
            Assert.NotNull(actual.LookupCKDAnswerEntity);
        }

        [Fact]
        public async Task Handle_WhenUrineNotCollected_WithReason_Member_Unable_With_Exclusive_Notes_SetsReason()
        {
            const int answerId = 50899;//answer Id for member unable.
            const string answerValue = "1";
            const int answerIdForNotes = 50900;//answer Id for notes when member unable.
            const string answerValueForNotes = "Unable to urinate at this time";
            const short pk = 2;

            await AddNotPerformedReasonToDb(pk, answerId);

            var answers = new List<EvaluationAnswerModel>
            {
                CreateUrineCollectedToday(false),
                CreateMemberUnableReason(answerId,answerValue),
                CreateMemberUnableReason(answerIdForNotes,answerValueForNotes)
            };

            A.CallTo(() => _evalApi.GetEvaluationVersion(A<int>._, A<string>._))
                .Returns(new EvaluationVersionRs { Evaluation = new EvaluationModel { Answers = answers } });

            var actual = await _checkCKDEvalHandler.Handle(new CheckCKDEval(), default);

            A.CallTo(() => _evalApi.GetEvaluationVersion(A<int>._, A<string>._))
                .MustHaveHappened();

            Assert.False(actual.IsCKDEvaluation);
            Assert.Equal(pk, actual.NotPerformedReasonId);
            Assert.Equal(answerId, actual.NotPerformedAnswerId);
            Assert.Equal(answerValueForNotes, actual.NotPerformedNotes);
        }

        #region Helpers
        private async Task AddNotPerformedReasonToDb(short pk, int answerId)
        {
            await _fixture.Context.NotPerformedReason.AddAsync(new NotPerformedReason
            {
                NotPerformedReasonId = pk,
                AnswerId = answerId
            });

            await _fixture.Context.SaveChangesAsync();
        }

        private static EvaluationAnswerModel CreateUrineCollectedToday(bool wasCollected)
            => new EvaluationAnswerModel
            {
                QuestionId = 463,
                AnswerId = wasCollected ? 20950 : 20949,
                AnswerValue = wasCollected ? "1" : "0"
            };

        private static EvaluationAnswerModel CreateNotPerformedReason(int answerId)
            => new EvaluationAnswerModel
            {
                QuestionId = 90654, // Corresponds to "Reason member refused"
                AnswerId = answerId
            };

        private static EvaluationAnswerModel CreateMemberUnableReason(int answerId, string answerValue)
          => new EvaluationAnswerModel
          {
              QuestionId = 90657, // Corresponds to "Reason member unable"
                AnswerId = answerId,
                AnswerValue = answerValue
          };

        private static EvaluationAnswerModel CreateUrineMicroalbuminDipstick(string answerValue)
            => new EvaluationAnswerModel
            {
                QuestionId = 468,
                AnswerId = 20962, // This is just one of the possible AnswerId's; the code doesn't actually look at this though, so it doesn't matter, just needs to be set
                AnswerValue = answerValue
            };

        private static EvaluationAnswerModel CreateExpiryDate(DateTime expiry)
            => new EvaluationAnswerModel
            {
                QuestionId = 91550,
                AnswerId = 33263,
                AnswerValue = expiry.ToString("d")
            };
        #endregion Helpers
    }
}
