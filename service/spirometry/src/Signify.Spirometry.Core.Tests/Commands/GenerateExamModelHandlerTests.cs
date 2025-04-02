using FakeItEasy;
using MediatR;
using Signify.Spirometry.Core.ApiClients.EvaluationApi.Responses;
using Signify.Spirometry.Core.Commands;
using Signify.Spirometry.Core.Exceptions;
using Signify.Spirometry.Core.Models;
using Signify.Spirometry.Core.Queries;
using Signify.Spirometry.Core.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Commands;

public class GenerateExamModelHandlerTests
{
    private const int WasSpirometryPerformedQ = 100291;

    private readonly IMediator _mediator = A.Fake<IMediator>();

    private GenerateExamModelHandler CreateSubject() => new(_mediator, new AnswerLookupBuilderService());

    [Fact]
    public async Task Handle_WithoutSpirometryTestPerformedQuestion_Throws()
    {
        // Arrange
        A.CallTo(() => _mediator.Send(A<QueryEvaluationModel>._, A<CancellationToken>._))
            .Returns(new EvaluationModel
            {
                Answers = new List<EvaluationAnswerModel>()
            });

        var subject = CreateSubject();

        // Act/Assert
        await Assert.ThrowsAnyAsync<RequiredEvaluationQuestionMissingException>(async () =>
        {
            await subject.Handle(new GenerateExamModel(1), default);
        });
    }

    [Fact]
    public async Task Handle_WithUnsupportedAnswerForSpirometryTestPerformed_Throws()
    {
        // Arrange
        A.CallTo(() => _mediator.Send(A<QueryEvaluationModel>._, A<CancellationToken>._))
            .Returns(new EvaluationModel
            {
                Answers = new List<EvaluationAnswerModel>
                {
                    new()
                    {
                        QuestionId = 100291, // "Spirometry test performed?"
                        AnswerId = 1 // Any invalid value that doesn't correspond to Yes or No
                    }
                }
            });

        var subject = CreateSubject();

        // Act/Assert
        await Assert.ThrowsAnyAsync<UnsupportedAnswerForQuestionException>(async () =>
        {
            await subject.Handle(new GenerateExamModel(1), default);
        });
    }

    [Theory]
    [MemberData(nameof(Handle_ReturnsProperResultModel_TestData))]
    public async Task Handle_ReturnsProperResultModel_Tests(List<EvaluationAnswerModel> answers, Type expectedResultType)
    {
        // Arrange
        const int formVersionId = 1;

        A.CallTo(() => _mediator.Send(A<QueryEvaluationModel>._, A<CancellationToken>._))
            .Returns(new EvaluationModel
            {
                FormVersionId = formVersionId,
                Answers = answers
            });

        A.CallTo(() => _mediator.Send(A<ParsePerformedResults>._, A<CancellationToken>._))
            .Returns(new PerformedExamModel(1, new RawExamResult()));
        A.CallTo(() => _mediator.Send(A<ParseNotPerformedResults>._, A<CancellationToken>._))
            .Returns(new NotPerformedExamModel(1, new NotPerformedInfo(NotPerformedReason.NotInterested)));

        var subject = CreateSubject();

        // Act
        var result = await subject.Handle(new GenerateExamModel(1), default);

        // Assert
        Assert.Equal(expectedResultType, result.GetType());
        Assert.Equal(formVersionId, result.FormVersionId);
    }

    public static IEnumerable<object[]> Handle_ReturnsProperResultModel_TestData()
    {
        yield return
        [
            new List<EvaluationAnswerModel>
            {
                new()
                {
                    QuestionId = WasSpirometryPerformedQ,
                    AnswerId = 50919 // Yes
                }
            },
            typeof(PerformedExamModel)
        ];

        yield return
        [
            new List<EvaluationAnswerModel>
            {
                new()
                {
                    QuestionId = WasSpirometryPerformedQ,
                    AnswerId = 50920 // No
                }
            },
            typeof(NotPerformedExamModel)
        ];
    }

    [Fact]
    public async Task Handle_WithMultipleAnswersToSameQuestion_IgnoresExtraAnswers()
    {
        // Arrange
        A.CallTo(() => _mediator.Send(A<QueryEvaluationModel>._, A<CancellationToken>._))
            .Returns(new EvaluationModel
            {
                Answers = new List<EvaluationAnswerModel>
                {
                    new()
                    {
                        QuestionId = WasSpirometryPerformedQ,
                        AnswerId = 50919 // Yes
                    },
                    new()
                    {
                        QuestionId = WasSpirometryPerformedQ,
                        AnswerId = 50920 // No
                    },
                    new()
                    {
                        QuestionId = WasSpirometryPerformedQ,
                        AnswerId = 1 // Any other answer id; value doesn't matter
                    },
                    new()
                    {
                        QuestionId = 1, // Value doesn't matter
                        AnswerId = 2 // Value doesn't matter, just different from the below
                    },
                    new()
                    {
                        QuestionId = 1, // Value doesn't matter
                        AnswerId = 3 // Value doesn't matter, just different from the above
                    }
                }
            });

        A.CallTo(() => _mediator.Send(A<ParsePerformedResults>._, A<CancellationToken>._))
            .Returns(new PerformedExamModel(1, new RawExamResult()));

        var subject = CreateSubject();

        // Act
        var result = await subject.Handle(new GenerateExamModel(1), default);

        // Assert
        Assert.Equal(typeof(PerformedExamModel), result.GetType());
        A.CallTo(() => _mediator.Send(A<ParsePerformedResults>.That.Matches(q =>
                    // Only two answers are supplied; others ignored
                    q.Answers.Count == 2 &&
                    // The answer to "Was spirometry performed?" is the first answer -- Yes
                    q.Answers[WasSpirometryPerformedQ].First().AnswerId == 50919),
                A<CancellationToken>._))
            .MustHaveHappened();
    }
}