using Signify.Spirometry.Core.ApiClients.EvaluationApi.Responses;
using Signify.Spirometry.Core.Services;
using System.Linq;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Services;

public class AnswerLookupBuilderServiceTests
{
    [Fact]
    public void BuildLookup_Test()
    {
        // Arrange
        var answers = new[]
        {
            new EvaluationAnswerModel {QuestionId = 1, AnswerId = 11},
            new EvaluationAnswerModel {QuestionId = 1, AnswerId = 11},
            new EvaluationAnswerModel {QuestionId = 1, AnswerId = 12},

            new EvaluationAnswerModel {QuestionId = 2, AnswerId = 13}
        };

        // Act
        var actual = new AnswerLookupBuilderService()
            .BuildLookup(answers);

        // Assert
        Assert.Equal(2, actual.Count);

        Assert.True(actual.TryGetValue(1, out var questionAnswers));
        Assert.Equal(3, questionAnswers.Count);
        Assert.Equal(2, questionAnswers.Count(answer => answer.AnswerId == 11));
        Assert.Equal(1, questionAnswers.Count(answer => answer.AnswerId == 12));

        Assert.True(actual.TryGetValue(2, out questionAnswers));
        Assert.Single(questionAnswers);
        Assert.Equal(13, questionAnswers.First().AnswerId);
    }
}