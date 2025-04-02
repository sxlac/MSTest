using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.Spirometry.Core.ApiClients.EvaluationApi.Responses;
using Signify.Spirometry.Core.Commands;
using Signify.Spirometry.Core.Exceptions;
using Signify.Spirometry.Core.Factories;
using Signify.Spirometry.Core.Models;
using Signify.Spirometry.Core.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Commands;

public class ParseNotPerformedResultsHandlerTests
{
    private const int EvaluationId = 1;

    private static readonly IBuildAnswerLookup LookupBuilder = new AnswerLookupBuilderService();

    private static ParseNotPerformedResultsHandler CreateSubject()
        => new(
            A.Dummy<ILogger<ParseNotPerformedResultsHandler>>(),
            new TrileanTypeConverterFactory());

    private static ParseNotPerformedResults CreateRequest(IEnumerable<EvaluationAnswerModel> answers) =>
        new(EvaluationId, LookupBuilder.BuildLookup(answers));

    [Theory]
    [MemberData(nameof(Handle_ValidatesAllRequiredAnswers_TestData))]
    public async Task Handle_ValidatesAllRequiredAnswers(IEnumerable<EvaluationAnswerModel> answers, int? expectedMissingQuestionId)
    {
        var subject = CreateSubject();

        try
        {
            await subject.Handle(CreateRequest(answers), default);

            // If there's an expected missing Q, it would have thrown and won't get here
            Assert.Null(expectedMissingQuestionId);
        }
        catch (RequiredEvaluationQuestionMissingException ex)
        {
            Assert.Equal(expectedMissingQuestionId, ex.QuestionId);
        }
        catch
        {
            // Ensures no other type of exception was thrown
            Assert.False(true);
        }
    }

    public static IEnumerable<object[]> Handle_ValidatesAllRequiredAnswers_TestData()
    {
        yield return
        [
            new[]
            {
                new EvaluationAnswerModel
                {
                    QuestionId = 100291, // Spirometry Test Performed
                    AnswerId = 50920 // No
                }
            },
            100292 // Q Missing: Reason Spirometry test not performed
        ];

        yield return
        [
            new[]
            {
                new EvaluationAnswerModel
                {
                    QuestionId = 100291, // Spirometry Test Performed
                    AnswerId = 50920 // No
                },
                new EvaluationAnswerModel
                {
                    QuestionId = 100292, // Reason Spirometry test not performed
                    AnswerId = 50921 // Member refused
                }
            },
            100293 // Q Missing: Reason member refused Spirometry testing
        ];

        yield return
        [
            new[]
            {
                new EvaluationAnswerModel
                {
                    QuestionId = 100291, // Spirometry Test Performed
                    AnswerId = 50920 // No
                },
                new EvaluationAnswerModel
                {
                    QuestionId = 100292, // Reason Spirometry test not performed
                    AnswerId = 50922 // Unable to perform
                }
            },
            100295 // Q: Reason unable to perform spirometry testing
        ];

        // Validate that if all required answers are supplied, no exception is thrown
        yield return
        [
            new[]
            {
                new EvaluationAnswerModel
                {
                    QuestionId = 100291, // Spirometry Test Performed
                    AnswerId = 50920 // No
                },
                new EvaluationAnswerModel
                {
                    QuestionId = 100292, // Reason Spirometry test not performed
                    AnswerId = 50921 // Member refused
                },
                new EvaluationAnswerModel
                {
                    QuestionId = 100293, // Reason member refused Spirometry testing
                    AnswerId = 50923 // Member recently completed
                }
            },
            null // No missing questions; should validate successfully
        ];

        // Validate that if all required answers are supplied, no exception is thrown
        yield return
        [
            new[]
            {
                new EvaluationAnswerModel
                {
                    QuestionId = 100291, // Spirometry Test Performed
                    AnswerId = 50920 // No
                },
                new EvaluationAnswerModel
                {
                    QuestionId = 100292, // Reason Spirometry test not performed
                    AnswerId = 50922 // Unable to perform
                },
                new EvaluationAnswerModel
                {
                    QuestionId = 100295, // Reason unable to perform spirometry testing
                    AnswerId = 50928 // Technical issue
                }
            },
            null // No missing questions; should validate successfully
        ];
    }

    [Theory]
    [MemberData(nameof(Handle_ValidatesSupportedAnswers_TestData))]
    public async Task Handle_ValidatesSupportedAnswers(IEnumerable<EvaluationAnswerModel> answers, EvaluationAnswerModel unsupportedAnswer)
    {
        var subject = CreateSubject();

        try
        {
            await subject.Handle(CreateRequest(answers), default);

            Assert.False(true); // Ensure we don't get here, it should have thrown
        }
        catch (UnsupportedAnswerForQuestionException ex)
        {
            Assert.Equal(unsupportedAnswer.QuestionId, ex.QuestionId); // Reason Spirometry test not performed
            Assert.Equal(unsupportedAnswer.AnswerId, ex.AnswerId);
            Assert.Equal(unsupportedAnswer.AnswerValue, ex.AnswerValue);
        }
        catch
        {
            // Ensures no other type of exception was thrown
            Assert.False(true);
        }
    }

    public static IEnumerable<object[]> Handle_ValidatesSupportedAnswers_TestData()
    {
        var notPerformed = new EvaluationAnswerModel
        {
            QuestionId = 100291, // Spirometry Test Performed
            AnswerId = 50920 // No
        };

        EvaluationAnswerModel BuildIncorrectAnswer(int questionId)
        {
            return new EvaluationAnswerModel
            {
                QuestionId = questionId,
                AnswerId = 1, // This isn't valid for any question
                AnswerValue = "doesn't matter"
            };
        }

        var invalidAnswer = BuildIncorrectAnswer(100292); // Not performed reason

        yield return
        [
            new[]
            {
                notPerformed,
                invalidAnswer
            },
            invalidAnswer
        ];

        invalidAnswer = BuildIncorrectAnswer(100293); // Reason member refused

        yield return
        [
            new[]
            {
                notPerformed,
                new EvaluationAnswerModel
                {
                    QuestionId = 100292, // Not performed reason
                    AnswerId = 50921 // Member refused
                },
                invalidAnswer
            },
            invalidAnswer
        ];

        invalidAnswer = BuildIncorrectAnswer(100295); // Reason unable to perform

        yield return
        [
            new[]
            {
                notPerformed,
                new EvaluationAnswerModel
                {
                    QuestionId = 100292, // Not performed reason
                    AnswerId = 50922 // Unable to perform
                },
                invalidAnswer
            },
            invalidAnswer
        ];

        invalidAnswer = BuildIncorrectAnswer(100294); // Notes

        yield return
        [
            new[]
            {
                notPerformed,
                new EvaluationAnswerModel
                {
                    QuestionId = 100292, // Not performed reason
                    AnswerId = 50922 // Unable to perform
                },
                new EvaluationAnswerModel
                {
                    QuestionId = 100295, // Reason unable to perform
                    AnswerId = 50929 // Environmental issue
                },
                invalidAnswer
            },
            invalidAnswer
        ];
    }

    [Theory]
    [MemberData(nameof(Handle_Tests_TestData))]
    [MemberData(nameof(Handle_ValidatesNotesLengthTest_TestData))]
    public async Task Handle_Tests(IEnumerable<EvaluationAnswerModel> answers, ExamModel expectedResult)
    {
        var subject = CreateSubject();

        var actual = await subject.Handle(CreateRequest(answers), default);

        Assert.Equal(expectedResult, actual);
    }

    private static readonly EvaluationAnswerModel SpirometryTestNotPerformed = new EvaluationAnswerModel
    {
        QuestionId = 100291, AnswerId = 50920
    };

    private static readonly EvaluationAnswerModel MemberRefused = new EvaluationAnswerModel
    {
        QuestionId = 100292, AnswerId = 50921
    };

    private static readonly EvaluationAnswerModel UnableToPerform = new EvaluationAnswerModel
    {
        QuestionId = 100292, AnswerId = 50922
    };

    public static IEnumerable<object[]> Handle_Tests_TestData()
    {
        var answers = new List<EvaluationAnswerModel>
        {
            SpirometryTestNotPerformed,
            MemberRefused,
            new()
            {
                QuestionId = 100293, // Reason member refused Spirometry testing
                AnswerId = 50923 // Member recently completed
            }
        };

        var expected = new NotPerformedExamModel(EvaluationId, new NotPerformedInfo(NotPerformedReason.MemberRecentlyCompleted));

        yield return [answers, expected];

        answers = new List<EvaluationAnswerModel>
        {
            SpirometryTestNotPerformed,
            MemberRefused,
            new()
            {
                QuestionId = 100293, // Reason member refused Spirometry testing
                AnswerId = 50924 // Scheduled to complete
            }
        };

        expected = new NotPerformedExamModel(EvaluationId, new NotPerformedInfo(NotPerformedReason.ScheduledToComplete));

        yield return [answers, expected];

        answers = new List<EvaluationAnswerModel>
        {
            SpirometryTestNotPerformed,
            MemberRefused,
            new()
            {
                QuestionId = 100293, // Reason member refused Spirometry testing
                AnswerId = 50925 // Member apprehension
            }
        };

        expected = new NotPerformedExamModel(EvaluationId, new NotPerformedInfo(NotPerformedReason.MemberApprehension));

        yield return [answers, expected];

        answers = new List<EvaluationAnswerModel>
        {
            SpirometryTestNotPerformed,
            MemberRefused,
            new EvaluationAnswerModel
            {
                QuestionId = 100293, // Reason member refused Spirometry testing
                AnswerId = 50926 // Not interested
            }
        };

        expected = new NotPerformedExamModel(EvaluationId, new NotPerformedInfo(NotPerformedReason.NotInterested));

        yield return [answers, expected];

        answers = new List<EvaluationAnswerModel>
        {
            SpirometryTestNotPerformed,
            UnableToPerform,
            new()
            {
                QuestionId = 100295, // Reason unable to perform spirometry testing
                AnswerId = 50928 // Technical issue
            }
        };

        expected = new NotPerformedExamModel(EvaluationId, new NotPerformedInfo(NotPerformedReason.TechnicalIssue));

        yield return [answers, expected];

        answers = new List<EvaluationAnswerModel>
        {
            SpirometryTestNotPerformed,
            UnableToPerform,
            new()
            {
                QuestionId = 100295, // Reason unable to perform spirometry testing
                AnswerId = 50929 // Environmental issue
            }
        };

        expected = new NotPerformedExamModel(EvaluationId, new NotPerformedInfo(NotPerformedReason.EnvironmentalIssue));

        yield return [answers, expected];

        answers = new List<EvaluationAnswerModel>
        {
            SpirometryTestNotPerformed,
            UnableToPerform,
            new()
            {
                QuestionId = 100295, // Reason unable to perform spirometry testing
                AnswerId = 50930 // No supplies or equipment
            }
        };

        expected = new NotPerformedExamModel(EvaluationId, new NotPerformedInfo(NotPerformedReason.NoSuppliesOrEquipment));

        yield return [answers, expected];

        answers = new List<EvaluationAnswerModel>
        {
            SpirometryTestNotPerformed,
            UnableToPerform,
            new()
            {
                QuestionId = 100295, // Reason unable to perform spirometry testing
                AnswerId = 50931 // Insufficient training
            }
        };

        expected = new NotPerformedExamModel(EvaluationId, new NotPerformedInfo(NotPerformedReason.InsufficientTraining));

        yield return [answers, expected];

        answers = new List<EvaluationAnswerModel>
        {
            SpirometryTestNotPerformed,
            UnableToPerform,
            new()
            {
                QuestionId = 100295, // Reason unable to perform spirometry testing
                AnswerId = 50932 // Member physically unable
            }
        };

        expected = new NotPerformedExamModel(EvaluationId, new NotPerformedInfo(NotPerformedReason.MemberPhysicallyUnable));

        yield return [answers, expected];

        answers = new List<EvaluationAnswerModel>
        {
            SpirometryTestNotPerformed,
            UnableToPerform,
            new EvaluationAnswerModel
            {
                QuestionId = 100295, // Reason unable to perform spirometry testing
                AnswerId = 50932 // Member physically unable
            },
            new EvaluationAnswerModel
            {
                QuestionId = 100294, // Member unable to perform notes, spirometry testing
                AnswerId = 50927, // Notes
                AnswerValue = null
            }
        };

        expected = new NotPerformedExamModel(EvaluationId, new NotPerformedInfo(NotPerformedReason.MemberPhysicallyUnable));

        yield return [answers, expected];

        answers = new List<EvaluationAnswerModel>
        {
            SpirometryTestNotPerformed,
            UnableToPerform,
            new()
            {
                QuestionId = 100295, // Reason unable to perform spirometry testing
                AnswerId = 50932 // Member physically unable
            },
            new()
            {
                QuestionId = 100294, // Member unable to perform notes, spirometry testing
                AnswerId = 50927, // Notes
                AnswerValue = "Notes"
            }
        };

        expected = new NotPerformedExamModel(EvaluationId, new NotPerformedInfo(NotPerformedReason.MemberPhysicallyUnable, "Notes"));

        yield return [answers, expected];

        answers = new List<EvaluationAnswerModel>
        {
            SpirometryTestNotPerformed,
            UnableToPerform,
            new()
            {
                QuestionId = 100295, // Reason unable to perform spirometry testing
                AnswerId = 50932 // Member physically unable
            },
            new()
            {
                QuestionId = 100294, // Member unable to perform notes, spirometry testing
                AnswerId = 50927, // Notes
                AnswerValue = "Some longer notes!123~"
            }
        };

        expected = new NotPerformedExamModel(EvaluationId, new NotPerformedInfo(NotPerformedReason.MemberPhysicallyUnable, "Some longer notes!123~"));

        yield return [answers, expected];

        answers = new List<EvaluationAnswerModel>
        {
            SpirometryTestNotPerformed,
            UnableToPerform,
            new()
            {
                QuestionId = 100295, // Reason unable to perform spirometry testing
                AnswerId = 51960 // Member outside demographic ranges
            }
        };

        expected = new NotPerformedExamModel(EvaluationId, new NotPerformedInfo(NotPerformedReason.MemberOutsideDemographicRanges));

        yield return [answers, expected];
    }

    public static IEnumerable<object[]> Handle_ValidatesNotesLengthTest_TestData()
    {
        var notes = new string('a', 1030);  //Test is to check that expected should be truncated to 1024 characters

        var notesExpected = notes.Substring(0, 1024); //Expected should be truncated to 1024 characters.

        var answers = new List<EvaluationAnswerModel>
        {
            SpirometryTestNotPerformed,
            UnableToPerform,
            new()
            {
                QuestionId = 100295, // Reason unable to perform spirometry testing
                AnswerId = 50932 // Member physically unable
            },
            new()
            {
                QuestionId = 100294, // Member unable to perform notes, spirometry testing
                AnswerId = 50927, // Notes
                AnswerValue = notes
            }
        };

        var expected = new NotPerformedExamModel(EvaluationId, new NotPerformedInfo(NotPerformedReason.MemberPhysicallyUnable, notesExpected));

        yield return [answers, expected];

        notes = "Random notes less than 1024 length";

        answers = new List<EvaluationAnswerModel>
        {
            SpirometryTestNotPerformed,
            UnableToPerform,
            new()
            {
                QuestionId = 100295, // Reason unable to perform spirometry testing
                AnswerId = 50932 // Member physically unable
            },
            new()
            {
                QuestionId = 100294, // Member unable to perform notes, spirometry testing
                AnswerId = 50927, // Notes
                AnswerValue = notes //Random Notes less than 1024 characters
            }
        };

        expected = new NotPerformedExamModel(EvaluationId, new NotPerformedInfo(NotPerformedReason.MemberPhysicallyUnable, notes));

        yield return [answers, expected];

        answers = new List<EvaluationAnswerModel>
        {
            SpirometryTestNotPerformed,
            UnableToPerform,
            new()
            {
                QuestionId = 100295, // Reason unable to perform spirometry testing
                AnswerId = 50932 // Member physically unable
            },
            new()
            {
                QuestionId = 100294, // Member unable to perform notes, spirometry testing
                AnswerId = 50927, // Notes
                AnswerValue = null
            }
        };

        expected = new NotPerformedExamModel(EvaluationId, new NotPerformedInfo(NotPerformedReason.MemberPhysicallyUnable));

        yield return [answers, expected];
    }
}