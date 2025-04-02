using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.PAD.Svc.Core.ApiClient.Response;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Models;
using Signify.PAD.Svc.Core.Services;
using Signify.PAD.Svc.Core.Tests.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Commands;

public class ParsePerformedResultsTests : IClassFixture<MockDbFixture>
{
    private readonly ParsePerformedResultsHandler _handler;

    public ParsePerformedResultsTests(MockDbFixture fixture)
    {
        _handler = new ParsePerformedResultsHandler(A.Dummy<ILogger<ParsePerformedResultsHandler>>(), fixture.Context);
    }

    [Fact]
    public async Task Handle_AlwaysSetsIsPadPerformedToday_ToTrue()
    {
        // Arrange
        var lookup = new AnswerLookupBuilderService()
            .BuildLookup(Enumerable.Empty<EvaluationAnswerModel>());

        var request = new ParsePerformedResults(default, lookup);

        // Act
        var actual = await _handler.Handle(request, default);

        // Assert
        Assert.True(actual.IsPadPerformedToday);
    }

    [Theory]
    [MemberData(nameof(Handle_WithPadPerformed_ResultTestsData))]
    public async Task Handle_WithPadPerformed_ResultTests(IEnumerable<EvaluationAnswerModel> answers, EvaluationAnswers expectedResult)
    {
        // Arrange
        var lookup = new AnswerLookupBuilderService()
            .BuildLookup(answers);

        var request = new ParsePerformedResults(default, lookup);

        // Act
        var actual = await _handler.Handle(request, default);

        // Assert
        Assert.Equal(expectedResult.LeftScore, actual.LeftScore);
        Assert.Equal(expectedResult.LeftScoreAnswerValue, actual.LeftScoreAnswerValue);
        Assert.Equal(expectedResult.LeftSeverity, actual.LeftSeverity);
        Assert.Equal(expectedResult.LeftNormalityIndicator, actual.LeftNormalityIndicator);
        Assert.Equal(expectedResult.LeftException, actual.LeftException);
        Assert.Equal(expectedResult.RightScore, actual.RightScore);
        Assert.Equal(expectedResult.RightScoreAnswerValue, actual.RightScoreAnswerValue);
        Assert.Equal(expectedResult.RightSeverity, actual.RightSeverity);
        Assert.Equal(expectedResult.RightNormalityIndicator, actual.RightNormalityIndicator);
        Assert.Equal(expectedResult.RightException, actual.RightException);
    }

    public static IEnumerable<object[]> Handle_WithPadPerformed_ResultTestsData()
    {
        IEnumerable<EvaluationAnswerModel> CreateAnswers(params (int questionId, int answerId, string answerValue)[] answers)
        {
            return answers.Select(each => new EvaluationAnswerModel
            {
                QuestionId = each.questionId,
                AnswerId = each.answerId,
                AnswerValue = each.answerValue
            });
        }

        #region Normal scenarios
        yield return
        [
            CreateAnswers(
                (90573, 29564, "1.25"), // PAD testing results (left)
                (90702, 30973, "1.30") // PAD testing results (right)
            ),
            new EvaluationAnswers
            {
                LeftScore = "1.25",
                LeftScoreAnswerValue = "1.25",
                LeftNormalityIndicator = "N",
                LeftSeverity = "Normal",
                RightScore = "1.30",
                RightScoreAnswerValue = "1.30",
                RightNormalityIndicator = "N",
                RightSeverity = "Normal"
            }
        ];

        yield return
        [
            CreateAnswers(
                (90573, 29564, "0.95"), // PAD testing results (left)
                (90702, 30973, "0.96") // PAD testing results (right)
            ),
            new EvaluationAnswers
            {
                LeftScore = "0.95",
                LeftScoreAnswerValue = "0.95",
                LeftNormalityIndicator = "N",
                LeftSeverity = "Borderline",
                RightScore = "0.96",
                RightScoreAnswerValue = "0.96",
                RightNormalityIndicator = "N",
                RightSeverity = "Borderline"
            }
        ];

        yield return
        [
            CreateAnswers(
                (90573, 29564, "0.23"), // PAD testing results (left)
                (90702, 30973, "0.35") // PAD testing results (right)
            ),
            new EvaluationAnswers
            {
                LeftScore = "0.23",
                LeftScoreAnswerValue = "0.23",
                LeftNormalityIndicator = "A",
                LeftSeverity = "Severe",
                RightScore = "0.35",
                RightScoreAnswerValue = "0.35",
                RightNormalityIndicator = "A",
                RightSeverity = "Moderate"
            }
        ];

        yield return
        [
            CreateAnswers(
                (90573, 26564, "0.66"), // PAD testing results (left)
                (90702, 30973, "1.00") // PAD testing results (right)
            ),
            new EvaluationAnswers
            {
                LeftScore = "0.66",
                LeftScoreAnswerValue = "0.66",
                LeftNormalityIndicator = "A",
                LeftSeverity = "Mild",
                RightScore = "1.00",
                RightScoreAnswerValue = "1.00",
                RightNormalityIndicator = "N",
                RightSeverity = "Normal"
            }
        ];
        #endregion Normal scenarios

        #region Edge case scenarios
        yield return
        [
            CreateAnswers(), // No answers
            new EvaluationAnswers
            {
                LeftNormalityIndicator = "U",
                LeftException = "Result not supplied",
                RightNormalityIndicator = "U",
                RightException = "Result not supplied"
            }
        ];

        yield return
        [
            CreateAnswers(
                (90573, 26564, " ") // PAD testing results (left)
            ),
            new EvaluationAnswers
            {
                LeftNormalityIndicator = "U",
                LeftException = "Result not supplied",
                RightNormalityIndicator = "U",
                RightException = "Result not supplied"
            }
        ];

        yield return
        [
            CreateAnswers(
                (90573, 26564, "not valid"), // PAD testing results (left)
                (90702, 30973, "99") // PAD testing results (right)
            ),
            new EvaluationAnswers
            {
                LeftScore = "not valid",
                LeftScoreAnswerValue = "not valid",
                LeftNormalityIndicator = "U",
                LeftException = "Result value malformed",
                RightScore = "99",
                RightScoreAnswerValue = "99",
                RightNormalityIndicator = "U",
                RightException = "Result value out of range"
            }
        ];

        yield return
        [
            CreateAnswers(
                (90573, 26564, "-0.01"), // PAD testing results (left)
                (90702, 30973, "001.223556") // PAD testing results (right)
            ),
            new EvaluationAnswers
            {
                LeftScore = "-0.01",
                LeftScoreAnswerValue = "-0.01",
                LeftNormalityIndicator = "U",
                LeftException = "Result value out of range",
                RightScore = "1.223556", // From our perspective, this is still valid
                RightScoreAnswerValue = "001.223556",
                RightNormalityIndicator = "N",
                RightSeverity = "Normal"
            }
        ];

        yield return
        [
            CreateAnswers(
                (90573, 26564, "0.9999"), // PAD testing results (left)
                (90702, 30973, "0.295") // PAD testing results (right)
            ),
            new EvaluationAnswers
            {
                LeftScoreAnswerValue = "0.9999",
                LeftNormalityIndicator = "N",
                LeftScore = "0.9999",
                LeftSeverity = "Borderline",
                RightScoreAnswerValue = "0.295",
                RightNormalityIndicator = "A",
                RightScore = "0.295",
                RightSeverity = "Severe"
            }
        ];

        yield return
        [
            CreateAnswers(
                (90573, 26564, "0"), // PAD testing results (left)
                (90702, 30973, "1.4") // PAD testing results (right)
            ),
            new EvaluationAnswers
            {
                LeftScore = "0",
                LeftScoreAnswerValue = "0",
                LeftNormalityIndicator = "A",
                LeftSeverity = "Severe",
                RightScore = "1.4",
                RightScoreAnswerValue = "1.4",
                RightNormalityIndicator = "N",
                RightSeverity = "Normal"
            }
        ];
        #endregion Edge case scenarios
    }
}