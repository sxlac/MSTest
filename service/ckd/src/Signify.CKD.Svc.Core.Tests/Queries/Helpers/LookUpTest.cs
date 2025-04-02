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
using System.Linq;
using LookUp = Signify.CKD.Svc.Core.Queries.Helpers.Lookup;
using Xunit;

namespace Signify.CKD.Svc.Core.Tests.Queries.Helpers
{
    public sealed class LookUpTest
    {
        //Test for the lookup class.
        [Theory]
        [MemberData(nameof(Handle_EvaluationAnswer_TestData))]
        public void LookUp_Dictionary_Inserts_Multiple_Answers_correctly(List<EvaluationAnswerModel> answers)
        {
            //Arrange
            const int answerId = 50899;//answer Id for member unable.
            const int answerIdForNotes = 50900;//answer Id for notes when member unable.
            var lookUp = new LookUp(answers);

            //Act
            var answersExists = lookUp.HasQuestion(90657, out var evalAnswers);
            var primaryAnswerExists = lookUp.HasQuestion(90657, answer => answer.AnswerId == 50899);

            //Assert
            Assert.True(answersExists);
            Assert.Equal(2, evalAnswers.Count());
            Assert.Equal(answerId, evalAnswers.First().AnswerId);
            evalAnswers.First(ans => ans.AnswerId == answerId).QuestionId.Should().Be(90657);
            evalAnswers.First(ans => ans.AnswerId == answerIdForNotes).QuestionId.Should().Be(90657);
        }

        #region Helpers

        public static IEnumerable<object[]> Handle_EvaluationAnswer_TestData()
        {
            static List<EvaluationAnswerModel> CreateEvaluationAnswerModel(EvaluationAnswerModel e1, EvaluationAnswerModel e2, EvaluationAnswerModel e3)
            {
                return new List<EvaluationAnswerModel>
                {
                      e1,e2,e3
                };
            }

            yield return new object[]
            {
                CreateEvaluationAnswerModel(
                    CreateMemberUnableReason(50899,"1"),
                    CreateUrineCollectedToday(false),
                    CreateMemberUnableReason(50900,"Unable to urinate at this time")
                  )
            };
            yield return new object[]
            {
                CreateEvaluationAnswerModel(
                    CreateUrineCollectedToday(false),
                    CreateMemberUnableReason(50899,"1"),
                    CreateMemberUnableReason(50900,"Unable to urinate at this time")
                  )
            };
            yield return new object[]
            {
                CreateEvaluationAnswerModel(
                    CreateMemberUnableReason(50900,"Unable to urinate at this time"),
                    CreateUrineCollectedToday(false),
                    CreateMemberUnableReason(50899,"1")
                  )
            };
        }

        private static EvaluationAnswerModel CreateUrineCollectedToday(bool wasCollected)
            => new()
            {
                QuestionId = 463,
                AnswerId = wasCollected ? 20950 : 20949,
                AnswerValue = wasCollected ? "1" : "0"
            };

        private static EvaluationAnswerModel CreateMemberUnableReason(int answerId, string answerValue)
          => new()
          {
              QuestionId = 90657, // Corresponds to "Reason member unable"
              AnswerId = answerId,
              AnswerValue = answerValue
          };
        #endregion Helpers
    }
}
