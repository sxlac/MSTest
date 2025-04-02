using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.Dps.Observability.Library.Services;
using Signify.PAD.Svc.Core.ApiClient.Response;
using Signify.PAD.Svc.Core.ApiClient;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Constants.Questions;
using Signify.PAD.Svc.Core.Exceptions;
using Signify.PAD.Svc.Core.Models;
using Signify.PAD.Svc.Core.Queries;
using Signify.PAD.Svc.Core.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Queries;

public class QueryEvaluationAnswersTests
{
    private readonly IEvaluationApi _evaluationApi = A.Fake<IEvaluationApi>();
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();
    private QueryEvaluationAnswersHandler CreateSubject()
        => new(A.Dummy<ILogger<QueryEvaluationAnswersHandler>>(), _evaluationApi, new AnswerLookupBuilderService(), _mediator, _publishObservability);

    private void SetupAnswers(IEnumerable<EvaluationAnswerModel> answers)
    {
        A.CallTo(() => _evaluationApi.GetEvaluationVersion(A<long>._, A<string>._))
            .Returns(new EvaluationVersionRs
            {
                Evaluation = new EvaluationModel
                {
                    Answers = new List<EvaluationAnswerModel>(answers)
                }
            });
    }

    [Theory]
    [MemberData(nameof(Handle_WhenNoAnswersReturned_Throws_TestData))]
    public async Task Handle_WhenNoAnswersReturned_Throws(EvaluationVersionRs apiResponse)
    {
        // Arrange
        var request = new QueryEvaluationAnswers
        {
            EvaluationId = 1
        };

        A.CallTo(() => _evaluationApi.GetEvaluationVersion(A<long>._, A<string>._))
            .Returns(apiResponse);

        // Act
        // Assert
        await Assert.ThrowsAsync<NoEvaluationAnswersExistException>(async () =>
            await CreateSubject().Handle(request, default));

        A.CallTo(() => _evaluationApi.GetEvaluationVersion(A<long>.That.Matches(i => i == request.EvaluationId), A<string>._))
            .MustHaveHappened();

        A.CallTo(_mediator)
            .MustNotHaveHappened();
    }

    public static IEnumerable<object[]> Handle_WhenNoAnswersReturned_Throws_TestData()
    {
        yield return new object[] { null };

        yield return new object[]
        {
            new EvaluationVersionRs
            {
                Evaluation = null
            }
        };

        yield return new object[]
        {
            new EvaluationVersionRs
            {
                Evaluation = new EvaluationModel
                {
                    Answers = null
                }
            }
        };

        yield return new object[]
        {
            new EvaluationVersionRs
            {
                Evaluation = new EvaluationModel
                {
                    Answers = new List<EvaluationAnswerModel>()
                }
            }
        };
    }

    /// <remarks>
    /// There's a question that precludes the PAd performed question from even being shown.
    /// If it is asked, then PAD was not performed.
    /// I've added both answer ids as inline data but they are irrelevant for unit tests.
    /// Leaving them to help document expected answers.
    /// </remarks>
    [Theory]
    [InlineData(52832)]
    [InlineData(52831)]
    public async Task Handle_WhenPadDiagnosisIsClinicallyMade_TreatsAsNotPerformed(int answerId)
    {
        // Arrange
        var request = new QueryEvaluationAnswers
        {
            EvaluationId = 1
        };

        SetupAnswers(new[]
        {
            new EvaluationAnswerModel()
            {
                AnswerId = answerId,
                QuestionId = 100660,
                AnswerValue = "true"
            }
        });

        var aoeSymptomResult = new AoeSymptomAnswers { LateralityCodeId = 1, FootPainDisappearsOtc = false, FootPainDisappearsWalkingOrDangling = true, PedalPulseCodeId = 1 };
        A.CallTo(() => _mediator.Send(A<ParseAoeSymptomResults>._, A<CancellationToken>._)).Returns(aoeSymptomResult);

        // Act
        var actual = await CreateSubject().Handle(request, default);

        // Assert
        Assert.NotNull(actual);
        Assert.False(actual.IsPadPerformedToday);
        Assert.Equal(PadDiagnosisConfirmedClinicallyQuestion.Reason, actual.NotPerformedReason);
        Assert.Equal(PadDiagnosisConfirmedClinicallyQuestion.Reason, actual.NotPerformedReasonType);
        Assert.Equal(string.Empty, actual.NotPerformedNotes);
        Assert.Equal(answerId, actual.NotPerformedAnswerId);
        Assert.Equal(1, actual.AoeSymptomAnswers.LateralityCodeId);

        // This next call must not happen because we won't parse the results when this question is answered.
        // It has its own reason type and notes that aren't parsed from the answers.

        A.CallTo(() => _mediator.Send(A<ParseNotPerformedResults>.That.Matches(p =>
                p.EvaluationId == request.EvaluationId &&
                p.Answers.Count == 1), A<CancellationToken>._))
            .MustNotHaveHappened();

        A.CallTo(() => _mediator.Send(A<ParsePerformedResults>._, A<CancellationToken>._))
            .MustNotHaveHappened();

        A.CallTo(() => _mediator.Send(A<ParseAoeSymptomResults>._, A<CancellationToken>._)).MustHaveHappened();
    }

    /// <remarks>
    /// This may later change; we probably don't want to assume it wasn't performed if
    /// the question was just not answered. At this time, though, we're getting a lot
    /// of VHRA, so leaving this as-is for now.
    /// </remarks>
    [Fact]
    public async Task Handle_WhenPadPerformedQuestionNotAnswered_TreatsAsNotPerformed()
    {
        // Arrange
        var request = new QueryEvaluationAnswers
        {
            EvaluationId = 1
        };

        SetupAnswers(new[]
        {
            new EvaluationAnswerModel() // Answer and values don't matter, just needs to have at least one answer 
        });

        var aoeSymptomResult = new AoeSymptomAnswers { LateralityCodeId = 1, FootPainDisappearsOtc = false, FootPainDisappearsWalkingOrDangling = true, PedalPulseCodeId = 1 };
        A.CallTo(() => _mediator.Send(A<ParseAoeSymptomResults>._, A<CancellationToken>._)).Returns(aoeSymptomResult);

        // Act
        var actual = await CreateSubject().Handle(request, default);

        // Assert
        Assert.NotNull(actual);

        A.CallTo(() => _mediator.Send(A<ParseNotPerformedResults>.That.Matches(p =>
                p.EvaluationId == request.EvaluationId &&
                p.Answers.Count == 1), A<CancellationToken>._))
            .MustHaveHappened();

        A.CallTo(() => _mediator.Send(A<ParsePerformedResults>._, A<CancellationToken>._))
            .MustNotHaveHappened();

        A.CallTo(() => _mediator.Send(A<ParseAoeSymptomResults>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Handle_WhenPadPerformedQuestionHasUnknownAnswer_Throws()
    {
        // Arrange
        var request = new QueryEvaluationAnswers();

        SetupAnswers(new[]
        {
            new EvaluationAnswerModel
            {
                QuestionId = 90572, // PAD Performed?
                AnswerId = 1 // Some unknown answer
            }
        });

        // Act
        // Assert
        await Assert.ThrowsAsync<UnsupportedAnswerForQuestionException>(async () =>
            await CreateSubject().Handle(request, default));

        A.CallTo(_mediator)
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task Handle_WhenParseAoeSymptomResultsIsEmpty_ReturnsNull()
    {
        // Arrange
        var request = new QueryEvaluationAnswers
        {
            EvaluationId = 1
        };

        SetupAnswers(new[]
        {
            new EvaluationAnswerModel()
            {
                AnswerId = 52832,
                QuestionId = 100660,
                AnswerValue = "true"
            }
        });

#nullable enable
        AoeSymptomAnswers? aoeSymptomResult = null;
#nullable disable
        A.CallTo(() => _mediator.Send(A<ParseAoeSymptomResults>._, A<CancellationToken>._)).Returns(aoeSymptomResult);

        // Act
        var actual = await CreateSubject().Handle(request, default);

        // Assert
        Assert.NotNull(actual);
        Assert.False(actual.IsPadPerformedToday);
        Assert.Equal(PadDiagnosisConfirmedClinicallyQuestion.Reason, actual.NotPerformedReason);
        Assert.Equal(PadDiagnosisConfirmedClinicallyQuestion.Reason, actual.NotPerformedReasonType);
        Assert.Equal(string.Empty, actual.NotPerformedNotes);
        Assert.Equal(52832, actual.NotPerformedAnswerId);
        Assert.Null(actual.AoeSymptomAnswers);

        // This next call must not happen because we won't parse the results when this question is answered.
        // It has its own reason type and notes that aren't parsed from the answers.

        A.CallTo(() => _mediator.Send(A<ParseNotPerformedResults>.That.Matches(p =>
                p.EvaluationId == request.EvaluationId &&
                p.Answers.Count == 1), A<CancellationToken>._))
            .MustNotHaveHappened();

        A.CallTo(() => _mediator.Send(A<ParsePerformedResults>._, A<CancellationToken>._))
            .MustNotHaveHappened();

        A.CallTo(() => _mediator.Send(A<ParseAoeSymptomResults>._, A<CancellationToken>._)).MustHaveHappened();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_WasPadPerformed_Tests(bool wasPerformed)
    {
        // Arrange
        var request = new QueryEvaluationAnswers
        {
            EvaluationId = 1
        };

        SetupAnswers(new[]
        {
            new EvaluationAnswerModel
            {
                QuestionId = 90572, // PAD Performed?
                AnswerId = wasPerformed ? 29560 : 29561
            }
        });

        var aoeSymptomResult = new AoeSymptomAnswers { LateralityCodeId = 1, FootPainDisappearsOtc = false, FootPainDisappearsWalkingOrDangling = true, PedalPulseCodeId = 1 };
        A.CallTo(() => _mediator.Send(A<ParseAoeSymptomResults>._, A<CancellationToken>._)).Returns(aoeSymptomResult);

        // Act
        var actual = await CreateSubject().Handle(request, default);

        // Assert
        Assert.NotNull(actual);
        A.CallTo(() => _mediator.Send(A<ParseAoeSymptomResults>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();

        if (wasPerformed)
        {
            A.CallTo(() => _mediator.Send(A<ParsePerformedResults>._, A<CancellationToken>._))
                .MustHaveHappened();
            A.CallTo(() => _mediator.Send(A<ParseNotPerformedResults>._, A<CancellationToken>._))
                .MustNotHaveHappened();
        }
        else
        {
            A.CallTo(() => _mediator.Send(A<ParsePerformedResults>._, A<CancellationToken>._))
                .MustNotHaveHappened();
            A.CallTo(() => _mediator.Send(A<ParseNotPerformedResults>._, A<CancellationToken>._))
                .MustHaveHappened();
        }
    }
}