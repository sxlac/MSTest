using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using Signify.PAD.Svc.Core.ApiClient.Response;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Commands;

public class ParseNotPerformedResultsTests
{
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();

    private ParseNotPerformedResultsHandler CreateSubject()
        => new(A.Dummy<ILogger<ParseNotPerformedResultsHandler>>(), _publishObservability);

    public enum NotPerformedReason
    {
        MemberRefused,
        UnableToPerform,
        NotClinicallyRelevant
    }

    private static EvaluationAnswerModel CreateNotPerformedReason(NotPerformedReason reason)
    {
        return new EvaluationAnswerModel
        {
            QuestionId = 90695, // Reason peripheral arterial disease testing not performed
            AnswerId = reason switch
            {
                NotPerformedReason.MemberRefused => 30957,
                NotPerformedReason.UnableToPerform => 30958,
                NotPerformedReason.NotClinicallyRelevant => 31125,
                _ => throw new Exception()
            }
        };
    }

    private static ParseNotPerformedResults CreateRequest(IEnumerable<EvaluationAnswerModel> answers)
        => new(default, new AnswerLookupBuilderService().BuildLookup(answers));

    [Fact]
    public async Task Handle_AlwaysSetsIsPadPerformedToday_ToFalse()
    {
        // Arrange
        var request = CreateRequest(Enumerable.Empty<EvaluationAnswerModel>());

        // Act
        var actual = await CreateSubject().Handle(request, default);

        // Assert
        Assert.False(actual.IsPadPerformedToday);
    }

    [Theory]
    // "Reason peripheral arterial disease testing not performed" question
    [InlineData(NotPerformedReason.NotClinicallyRelevant, 90695, 31125)] // Not clinically relevant
    // "Reason member refused peripheral arterial disease" question (sub-question for "Member refused" answer)
    [InlineData(NotPerformedReason.MemberRefused, 90696, 30959)] // Member recently completed
    [InlineData(NotPerformedReason.MemberRefused, 90696, 30960)] // Scheduled to complete
    [InlineData(NotPerformedReason.MemberRefused, 90696, 30961)] // Member apprehension
    [InlineData(NotPerformedReason.MemberRefused, 90696, 30962)] // Not interested
    [InlineData(NotPerformedReason.MemberRefused, 90696, 30963)] // Other
    // "Reason unable to perform peripheral arterial disease testing" question (sub-question for "Unable to perform" answer)
    [InlineData(NotPerformedReason.UnableToPerform, 90699, 30966)] // Technical issue
    [InlineData(NotPerformedReason.UnableToPerform, 90699, 30967)] // Environmental issue
    [InlineData(NotPerformedReason.UnableToPerform, 90699, 30968)] // No supplies or equipment
    [InlineData(NotPerformedReason.UnableToPerform, 90699, 30969)] // Insufficient training
    [InlineData(NotPerformedReason.UnableToPerform, 90699, 50917)] // Member physically unable
    public async Task Handle_WithPadNotPerformed_WithReason_ShouldSetNotPerformedAnswerId(
        NotPerformedReason reason, int questionId, int answerId)
    {
        // Arrange
        var request = CreateRequest(new List<EvaluationAnswerModel>
        {
            CreateNotPerformedReason(reason),
            new()
            {
                QuestionId = questionId,
                AnswerId = answerId
            }
        });

        // Act
        var actual = await CreateSubject().Handle(request, default);

        // Assert
        Assert.False(actual.IsPadPerformedToday);
        Assert.Equal(answerId, actual.NotPerformedAnswerId);
    }

    [Theory]
    // "Reason peripheral arterial disease testing not performed" question
    [InlineData(90695, 30957)] // Member refused
    [InlineData(90695, 30958)] // Unable to perform
    // "Reason member refused peripheral arterial disease" question
    [InlineData(90696, 30964)] // Member refusal notes
    // "Reason unable to perform peripheral arterial disease testing" question
    [InlineData(90699, 30971)] // Member unable to perform notes
    [InlineData(90699, 50918)] // Member physically unable notes
    // Other
    [InlineData(1, 1)]
    [InlineData(60697, 30964)]
    [InlineData(60700, 30971)]
    public async Task Handle_WithPadNotPerformed_WithInvalidReason_ShouldNotSetNotPerformedAnswerId(
        int questionId, int answerId)
    {
        // Arrange
        var request = CreateRequest(new List<EvaluationAnswerModel>
        {
            new()
            {
                QuestionId = questionId,
                AnswerId = answerId
            }
        });

        // Act
        var actual = await CreateSubject().Handle(request, default);

        // Assert
        Assert.False(actual.IsPadPerformedToday);
        Assert.Null(actual.NotPerformedAnswerId);
    }

    [Theory]
    // "Reason member refused peripheral arterial disease" question (sub-question for "Member refused" answer)
    [InlineData(NotPerformedReason.MemberRefused, 90696, 30959, 90697, 30964, "Member refused")] // Member recently completed
    // "Reason unable to perform peripheral arterial disease testing" question (sub-question for "Unable to perform" answer)
    [InlineData(NotPerformedReason.UnableToPerform, 90699, 30966, 90700, 30971, "Member Unable")] // Technical issue
    [InlineData(NotPerformedReason.NotClinicallyRelevant, 90695, 31125, 90695, 31126, "Not clinically relevant")] // Not Clinically Relevant with specific notes
    public async Task Handle_WithPadNotPerformed_WithNotes_ShouldSetNotPerformedAnswerId(
        NotPerformedReason reason, int questionId, int answerId, int notesQuestionId, int notesAnswerId, string notes)
    {
        // Arrange
        var request = CreateRequest(new List<EvaluationAnswerModel>
        {
            CreateNotPerformedReason(reason),
            new()
            {
                QuestionId = questionId,
                AnswerId = answerId
            },
            new()
            {
                QuestionId = notesQuestionId,
                AnswerId = notesAnswerId,
                AnswerValue = notes
            }
        });

        // Act
        var actual = await CreateSubject().Handle(request, default);

        // Assert
        Assert.False(string.IsNullOrEmpty(actual.NotPerformedNotes));
        Assert.False(actual.IsPadPerformedToday);
    }

    [Theory]
    [InlineData(NotPerformedReason.MemberRefused, 90697, 30964)] // Member refused notes Q/A
    [InlineData(NotPerformedReason.UnableToPerform, 90700, 30971)] // Unable to perform notes Q/A
    [InlineData(NotPerformedReason.NotClinicallyRelevant, 90695, 31126)] // Not clinically relevant notes Q/A
    public async Task Handle_WhenNotPerformed_Notes_MaxLengthTests(NotPerformedReason reason, int notesQuestionId, int notesAnswerId)
    {
        // Arrange
        const int maxLength = 1024;

        foreach (var length in new[] {0, 1, 1000, 1023, 1024, 1025, 5000})
        {
            var request = CreateRequest(new List<EvaluationAnswerModel>
            {
                CreateNotPerformedReason(reason),
                new()
                {
                    QuestionId = notesQuestionId,
                    AnswerId = notesAnswerId,
                    AnswerValue = new string('a', length)
                }
            });

            // Act
            var actual = await CreateSubject().Handle(request, default);

            // Assert
            Assert.Equal(Math.Min(maxLength, actual.NotPerformedNotes.Length), actual.NotPerformedNotes.Length);
        }
    }

    [Theory]
    [InlineData(90699, 30966, 90700, 30971, "Not performed unable technical issue notes")] // Not performed unable notes to make sure we are not getting notes from the wrong reason.
    public async Task Handle_WithPadNotPerformed_WithNotPerformedUnableNotes(
        int questionId, int answerId, int notesQuestionId, int notesAnswerId, string notes)
    {
        // Arrange
        var request = CreateRequest(new List<EvaluationAnswerModel>
        {
            CreateNotPerformedReason(NotPerformedReason.UnableToPerform),
            new()
            {
                QuestionId = questionId,
                AnswerId = answerId
            },
            new()
            {
                QuestionId = notesQuestionId,
                AnswerId = notesAnswerId,
                AnswerValue = notes
            }
        });

        // Act
        var actual = await CreateSubject().Handle(request, default);

        // Assert
        Assert.False(string.IsNullOrEmpty(actual.NotPerformedNotes));

        Assert.Equal("Not performed unable technical issue notes", actual.NotPerformedNotes);
        Assert.Equal("Technical issue", actual.NotPerformedReason);
        Assert.Equal("Unable to perform", actual.NotPerformedReasonType);
        Assert.False(actual.IsPadPerformedToday);
    }

    [Fact]
    public async Task Handle_WhenNotPerformed_NotClinicallyRelevantNotes_OnlyReferenced_WhenNotClinicallyRelevant()
    {
        // Arrange
        const string notes = nameof(notes);

        foreach (var reason in Enum.GetValues<NotPerformedReason>())
        {
            var request = CreateRequest(new List<EvaluationAnswerModel>
            {
                CreateNotPerformedReason(reason),
                new()
                {
                    QuestionId = 90695, // Reason peripheral arterial disease testing not performed
                    AnswerId = 31126, // Reason not clinically relevant
                    AnswerValue = notes
                }
            });

            // Act
            var actual = await CreateSubject().Handle(request, default);

            // Assert
            var expectedNotes = reason == NotPerformedReason.NotClinicallyRelevant
                ? notes
                : string.Empty;

            Assert.Equal(expectedNotes, actual.NotPerformedNotes);
        }
    }

    [Theory]
    [InlineData(90695, 31126, "Not clinically relevant notes")] // Not Clinically Relevant with specific notes
    public async Task Handle_WithPadNotPerformed_WithNotClinicallyRelevantNotes(
        int notesQuestionId, int notesAnswerId, string notes)
    {
        // Arrange
        var request = CreateRequest(new List<EvaluationAnswerModel>
        {
            CreateNotPerformedReason(NotPerformedReason.NotClinicallyRelevant),
            new()
            {
                QuestionId = notesQuestionId,
                AnswerId = notesAnswerId,
                AnswerValue = notes
            }
        });

        // Act
        var actual = await CreateSubject().Handle(request, default);

        // Assert
        Assert.False(string.IsNullOrEmpty(actual.NotPerformedNotes));

        Assert.Equal("Not clinically relevant notes", actual.NotPerformedNotes);
        Assert.Equal("Not clinically relevant", actual.NotPerformedReason);
        Assert.Equal("Not clinically relevant", actual.NotPerformedReasonType);
        Assert.False(actual.IsPadPerformedToday);
    }

    [Theory]
    // "Reason member refused peripheral arterial disease" question (sub-question for "Member refused" answer)
    [InlineData(NotPerformedReason.MemberRefused, 90696, 30959, "Member recently completed")]
    // "Reason unable to perform peripheral arterial disease testing" question (sub-question for "Unable to perform" answer)
    [InlineData(NotPerformedReason.UnableToPerform, 90699, 30966, "Technical issue")]
    public async Task Handle_WithPadNotPerformed_WithReasons_ShouldSetNotPerformedAnswerId(
        NotPerformedReason reason, int questionId, int answerId, string reasonText)
    {
        // Arrange
        var request = CreateRequest(new List<EvaluationAnswerModel>
        {
            CreateNotPerformedReason(reason),
            new()
            {
                QuestionId = questionId,
                AnswerId = answerId,
                AnswerValue = reasonText
            }
        });

        // Act
        var actual = await CreateSubject().Handle(request, default);

        // Assert
        Assert.False(string.IsNullOrEmpty(actual.NotPerformedReason));
        Assert.False(string.IsNullOrEmpty(actual.NotPerformedReasonType));
        Assert.False(actual.IsPadPerformedToday);
    }

    [Fact]
    public async Task Handle_WithNotPerformedForTechIssue_SendObservabilityMessage()
    {
        // Arrange
        var request = CreateRequest(
        [
            CreateNotPerformedReason(NotPerformedReason.UnableToPerform),
            new()
            {
                QuestionId = 90699,
                AnswerId = 30966 // Technical issue
            }
        ]);

        // Act
        await CreateSubject().Handle(request, default);

        // Assert
        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>._, true)).MustHaveHappenedOnceExactly();
    }

    [Theory]
    [InlineData(NotPerformedReason.NotClinicallyRelevant, 90695, 31125)] // Not clinically relevant
    [InlineData(NotPerformedReason.MemberRefused, 90696, 30959)] // Member recently completed
    [InlineData(NotPerformedReason.MemberRefused, 90696, 30960)] // Scheduled to complete
    [InlineData(NotPerformedReason.MemberRefused, 90696, 30961)] // Member apprehension
    [InlineData(NotPerformedReason.MemberRefused, 90696, 30962)] // Not interested
    [InlineData(NotPerformedReason.MemberRefused, 90696, 30963)] // Other
    [InlineData(NotPerformedReason.UnableToPerform, 90699, 30967)] // Environmental issue
    [InlineData(NotPerformedReason.UnableToPerform, 90699, 30968)] // No supplies or equipment
    [InlineData(NotPerformedReason.UnableToPerform, 90699, 30969)] // Insufficient training
    [InlineData(NotPerformedReason.UnableToPerform, 90699, 50917)] // Member physically unable
    public async Task Handle_WithNonNotPerformedForTechIssue_DoNotSendObservabilityMessage(NotPerformedReason reason, int questionId, int answerId)
    {
        // Arrange
        var request = CreateRequest(
        [
            CreateNotPerformedReason(reason),
            new()
            {
                QuestionId = questionId,
                AnswerId = answerId
            }
        ]);

        // Act
        await CreateSubject().Handle(request, default);

        // Assert
        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>._, true)).MustNotHaveHappened();
    }
}