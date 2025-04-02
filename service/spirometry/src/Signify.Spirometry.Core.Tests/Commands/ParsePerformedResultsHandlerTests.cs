using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.Spirometry.Core.ApiClients.EvaluationApi.Responses;
using Signify.Spirometry.Core.Commands;
using Signify.Spirometry.Core.Exceptions;
using Signify.Spirometry.Core.Factories;
using Signify.Spirometry.Core.Models;
using Signify.Spirometry.Core.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Commands;

public class ParsePerformedResultsHandlerTests
{
    private const int EvaluationId = 1;

    private static readonly IBuildAnswerLookup LookupBuilder = new AnswerLookupBuilderService();

    private static ParsePerformedResultsHandler CreateSubject()
        => new(
            A.Dummy<ILogger<ParsePerformedResultsHandler>>(),
            new TrileanTypeConverterFactory(),
            new OccurrenceFrequencyConverterFactory());

    private static ParsePerformedResults CreateRequest(IEnumerable<EvaluationAnswerModel> answers) =>
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
        // These are in the order they are parsed
        var allRequiredAnswers = new List<EvaluationAnswerModel>
        {
            new()
            {
                QuestionId = 100297, // Session Grade
                AnswerId = 50938 // B
            }
        };

        for (var i = 0; i < allRequiredAnswers.Count - 1 /*all but the last required Q*/; ++i)
        {
            yield return [allRequiredAnswers.Take(i), allRequiredAnswers[i].QuestionId];
        }

        // Validate that if all required answers are supplied, no exception is thrown
        yield return [allRequiredAnswers, null];
    }

    [Theory]
    [MemberData(nameof(Build_WithExamPerformedAnswers_Tests_TestData))]
    public async Task Build_WithExamPerformedAnswers_Tests(IEnumerable<EvaluationAnswerModel> answers, ExamModel expectedResult)
    {
        var subject = CreateSubject();

        var actual = await subject.Handle(CreateRequest(answers), default);

        Assert.Equal(expectedResult, actual);
    }

    private static readonly EvaluationAnswerModel SpirometryTestPerformed = new EvaluationAnswerModel
    {
        QuestionId = 100291, AnswerId = 50919
    };

    public static IEnumerable<object[]> Build_WithExamPerformedAnswers_Tests_TestData()
    {
        string fvc = "35";
        string fev1 = "43";
        string fevOverFvc = "0.37";

        var answers = new List<EvaluationAnswerModel>
        {
            SpirometryTestPerformed,
            new() // Session Grade = A
            {
                QuestionId = 100297, AnswerId = 50937
            },
            new() // FVC
            {
                QuestionId = 100311, AnswerValue = fvc
            },
            new() // FEV1
            {
                QuestionId = 100312, AnswerValue = fev1
            },
            new() // FEV/FVC
            {
                QuestionId = 100298, AnswerValue = fevOverFvc
            },
            new() // Symptom = No
            {
                QuestionId = 100299, AnswerId = 50945
            },
            new() // Env or Risk = Yes
            {
                QuestionId = 100300, AnswerId = 50947
            },
            new() // Comorbidity = Unknown
            {
                QuestionId = 100301, AnswerId = 50952
            }
            // No COPD diagnosis
        };

        var expected = new PerformedExamModel(EvaluationId,
            new RawExamResult
            {
                EvaluationId = EvaluationId,
                SessionGrade = SessionGrade.A,
                Fvc = fvc,
                Fev1 = fev1,
                Fev1FvcRatio = fevOverFvc,
                HasHighSymptom = TrileanType.No,
                HasEnvOrExpRisk = TrileanType.Yes,
                HasHighComorbidity = TrileanType.Unknown,
                CopdDiagnosis = null
            });

        yield return [answers, expected];

        fvc = "113";
        fev1 = "101";
        fevOverFvc = "0.99";

        answers = new List<EvaluationAnswerModel>
        {
            SpirometryTestPerformed,
            new() // Session Grade = E
            {
                QuestionId = 100297, AnswerId = 50941
            },
            new() // FVC
            {
                QuestionId = 100311, AnswerValue = fvc
            },
            new() // FEV1
            {
                QuestionId = 100312, AnswerValue = fev1
            },
            new() // FEV/FVC
            {
                QuestionId = 100298, AnswerValue = fevOverFvc
            },
            new() // Symptom = Unknown
            {
                QuestionId = 100299, AnswerId = 50946
            },
            new() // Env or Risk = No
            {
                QuestionId = 100300, AnswerId = 50948
            },
            new() // Comorbidity = Yes
            {
                QuestionId = 100301, AnswerId = 50950
            }
            // No COPD diagnosis
        };

        expected = new PerformedExamModel(EvaluationId,
            new RawExamResult
            {
                EvaluationId = EvaluationId,
                SessionGrade = SessionGrade.E,
                Fvc = fvc,
                Fev1 = fev1,
                Fev1FvcRatio = fevOverFvc,
                HasHighSymptom = TrileanType.Unknown,
                HasEnvOrExpRisk = TrileanType.No,
                HasHighComorbidity = TrileanType.Yes,
                CopdDiagnosis = null
            });

        yield return [answers, expected];

        fvc = "25";
        fev1 = "125";
        fevOverFvc = "1.00";

        answers = new List<EvaluationAnswerModel>
        {
            SpirometryTestPerformed,
            new() // Session Grade = C
            {
                QuestionId = 100297, AnswerId = 50939
            },
            new() // FVC
            {
                QuestionId = 100311, AnswerValue = fvc
            },
            new() // FEV1
            {
                QuestionId = 100312, AnswerValue = fev1
            },
            new() // FEV/FVC
            {
                QuestionId = 100298, AnswerValue = fevOverFvc
            },
            new() // Symptom = Yes
            {
                QuestionId = 100299, AnswerId = 50944
            },
            new() // Env or Risk = Yes
            {
                QuestionId = 100300, AnswerId = 50947
            },
            new() // Comorbidity = Yes
            {
                QuestionId = 100301, AnswerId = 50950
            },
            new() // COPD Diagnosis = Yes
            {
                QuestionId = 100307, AnswerId = 50993
            }
        };

        expected = new PerformedExamModel(EvaluationId,
            new RawExamResult
            {
                EvaluationId = EvaluationId,
                SessionGrade = SessionGrade.C,
                Fvc = fvc,
                Fev1 = fev1,
                Fev1FvcRatio = fevOverFvc,
                HasHighSymptom = TrileanType.Yes,
                HasEnvOrExpRisk = TrileanType.Yes,
                HasHighComorbidity = TrileanType.Yes,
                CopdDiagnosis = true
            });

        yield return [answers, expected];

        fvc = "25";
        fev1 = "125";
        fevOverFvc = "1.00";

        answers = new List<EvaluationAnswerModel>
        {
            SpirometryTestPerformed,
            new() // Session Grade = F
            {
                QuestionId = 100297, AnswerId = 50942
            },
            new() // FVC
            {
                QuestionId = 100311, AnswerValue = fvc
            },
            new() // FEV1
            {
                QuestionId = 100312, AnswerValue = fev1
            },
            new() // FEV/FVC
            {
                QuestionId = 100298, AnswerValue = fevOverFvc
            },
            new() // Symptom = No
            {
                QuestionId = 100299, AnswerId = 50945
            },
            new() // Env or Risk = No
            {
                QuestionId = 100300, AnswerId = 50948
            },
            new() // Comorbidity = No
            {
                QuestionId = 100301, AnswerId = 50951
            }
            // No COPD diagnosis
        };

        expected = new PerformedExamModel(EvaluationId,
            new RawExamResult
            {
                EvaluationId = EvaluationId,
                SessionGrade = SessionGrade.F,
                Fvc = fvc,
                Fev1 = fev1,
                Fev1FvcRatio = fevOverFvc,
                HasHighSymptom = TrileanType.No,
                HasEnvOrExpRisk = TrileanType.No,
                HasHighComorbidity = TrileanType.No,
                CopdDiagnosis = null
            });

        yield return [answers, expected];

        fvc = "";
        fev1 = "";
        fevOverFvc = "";

        answers = new List<EvaluationAnswerModel>
        {
            SpirometryTestPerformed,
            new() // Session Grade = F
            {
                QuestionId = 100297, AnswerId = 50942
            },
            new() // FVC
            {
                QuestionId = 100311, AnswerValue = fvc
            },
            new() // FEV1
            {
                QuestionId = 100312, AnswerValue = fev1
            },
            new() // FEV/FVC
            {
                QuestionId = 100298, AnswerValue = fevOverFvc
            },
            new() // Symptom = No
            {
                QuestionId = 100299, AnswerId = 50945
            },
            new() // Env or Risk = No
            {
                QuestionId = 100300, AnswerId = 50948
            },
            new() // Comorbidity = No
            {
                QuestionId = 100301, AnswerId = 50951
            }
            // No COPD diagnosis
        };

        expected = new PerformedExamModel(EvaluationId,
            new RawExamResult
            {
                EvaluationId = EvaluationId,
                SessionGrade = SessionGrade.F,
                Fvc = fvc,
                Fev1 = fev1,
                Fev1FvcRatio = fevOverFvc,
                HasHighSymptom = TrileanType.No,
                HasEnvOrExpRisk = TrileanType.No,
                HasHighComorbidity = TrileanType.No,
                CopdDiagnosis = null
            });

        yield return [answers, expected];

        fvc = null;
        fev1 = null;
        fevOverFvc = null;

        answers = new List<EvaluationAnswerModel>
        {
            SpirometryTestPerformed,
            new() // Session Grade = F
            {
                QuestionId = 100297, AnswerId = 50942
            },
            // Verify that if any of these questions are not answered, it is still able to properly build the result
#pragma warning disable S125 // SonarQube: Sections of code should not be commented out - Leaving here to show readers explicitly what questions are not being answered 
            /*
            new() // FVC
            {
                QuestionId = 100311, AnswerValue = fvc
            },
            new() // FEV1
            {
                QuestionId = 100312, AnswerValue = fev1
            },
            new() // FEV/FVC
            {
                QuestionId = 100298, AnswerValue = fevOverFvc
            },
            */
#pragma warning restore S125
            new() // Symptom = No
            {
                QuestionId = 100299, AnswerId = 50945
            },
            new() // Env or Risk = No
            {
                QuestionId = 100300, AnswerId = 50948
            },
            new() // Comorbidity = No
            {
                QuestionId = 100301, AnswerId = 50951
            }
            // No COPD diagnosis
        };

        expected = new PerformedExamModel(EvaluationId,
            new RawExamResult
            {
                EvaluationId = EvaluationId,
                SessionGrade = SessionGrade.F,
                Fvc = fvc,
                Fev1 = fev1,
                Fev1FvcRatio = fevOverFvc,
                HasHighSymptom = TrileanType.No,
                HasEnvOrExpRisk = TrileanType.No,
                HasHighComorbidity = TrileanType.No,
                CopdDiagnosis = null
            });

        yield return new object[] {answers, expected};

        #region Lung function results
        answers = new List<EvaluationAnswerModel>
        {
            new() // Have smoked tobacco = Yes
            {
                QuestionId = 90, AnswerId = 20486
            },
            new() // Total years smoking = 36
            {
                QuestionId = 2001, AnswerId = 21211, AnswerValue = "36"
            },
            new() // Produce sputum with cough = No
            {
                QuestionId = 221, AnswerId = 20723
            },
            new() // How often cough up mucus = Never
            {
                QuestionId = 100402, AnswerId = 51405
            },
            new() // Have you had wheezing = Unknown
            {
                QuestionId = 87, AnswerId = 33594
            },
            new() // Get short of breath at rest = No
            {
                QuestionId = 97, AnswerId = 20497
            },
            new() // Get short of breath mild exertion = Yes
            {
                QuestionId = 98, AnswerId = 20500
            },
            new() // How often chest noisy = Sometimes
            {
                QuestionId = 100403, AnswerId = 51412
            },
            new() // How often during physical = Often
            {
                QuestionId = 100404, AnswerId = 51418
            },
            new() // Lung function score = 19
            {
                QuestionId = 100405, AnswerId = 51420, AnswerValue = "19"
            }
        };

        expected = new PerformedExamModel(EvaluationId,
            new RawExamResult
            {
                EvaluationId = EvaluationId,
                HasSmokedTobacco = true,
                TotalYearsSmoking = 36,
                ProducesSputumWithCough = false,
                CoughMucusFrequency = OccurrenceFrequency.Never,
                HadWheezingPast12mo = TrileanType.Unknown,
                GetsShortnessOfBreathAtRest = TrileanType.No,
                GetsShortnessOfBreathWithMildExertion = TrileanType.Yes,
                NoisyChestFrequency = OccurrenceFrequency.Sometimes,
                ShortnessOfBreathPhysicalActivityFrequency = OccurrenceFrequency.Often,
                LungFunctionQuestionnaireScore = 19
            });

        yield return new object[] {answers, expected};
        #endregion Lung function results
    }

    [Fact]
    public async Task Handle_WithInvalidHasSmokedTobaccoAnswerId_ThrowsUnsupportedAnswer()
    {
        var answers = new List<EvaluationAnswerModel>
        {
            new()
            {
                QuestionId = 90, // Has smoked tobacco
                AnswerId = 1, // Anything invalid
                AnswerValue = "test" // Value doesn't matter, and we don't ever care about the answer value for this Q, but to validate against the thrown ex
            }
        };

        var subject = CreateSubject();

        var ex = await Assert.ThrowsAnyAsync<UnsupportedAnswerForQuestionException>(async () =>
            await subject.Handle(CreateRequest(answers), default));

        Assert.Equal(90, ex.QuestionId);
        Assert.Equal(1, ex.AnswerId);
        Assert.Equal("test", ex.AnswerValue);
    }

    [Theory]
    [InlineData(90, 1, "test")] // Have you ever smoked tobacco
    [InlineData(221, 2, "something else")] // Do you produce sputum with your cough
    public async Task Handle_WithInvalidOptionalBoolAnswerId_ThrowsUnsupportedAnswer(int questionId, int answerId, string answerValue)
    {
        var answers = new List<EvaluationAnswerModel>
        {
            new()
            {
                QuestionId = questionId,
                AnswerId = answerId,
                AnswerValue = answerValue
            }
        };

        var subject = CreateSubject();

        var ex = await Assert.ThrowsAnyAsync<UnsupportedAnswerForQuestionException>(async () =>
            await subject.Handle(CreateRequest(answers), default));

        Assert.Equal(questionId, ex.QuestionId);
        Assert.Equal(answerId, ex.AnswerId);
        Assert.Equal(answerValue, ex.AnswerValue);
    }

    [Theory]
    [InlineData("0", 0)]
    [InlineData("1", 1)]
    [InlineData("13", 13)]
    public async Task Handle_TotalYearsSmoking_ValidAnswer_Tests(string answerValue, int expectedYears)
    {
        var answers = new List<EvaluationAnswerModel>
        {
            new()
            {
                QuestionId = 2001,
                AnswerId = 21211,
                AnswerValue = answerValue
            }
        };

        var subject = CreateSubject();

        var actual = await subject.Handle(CreateRequest(answers), default);

        Assert.Equal(expectedYears, actual.ExamResult.TotalYearsSmoking);
    }

    [Theory]
    // Total years smoking
    [InlineData(2001, 21211, "")]
    [InlineData(2001, 21211, "not a number")]
    [InlineData(2001, 21211, "!")]
    [InlineData(2001, 21211, " ")]
    // Lung function score
    [InlineData(100405, 51420, "~")]
    [InlineData(100405, 51420, "3.6")]
    [InlineData(100405, 51420, "Signify")]
    public async Task Handle_WithInvalidIntegerAnswerValue_Tests(int questionId, int answerId, string invalidAnswerValue)
    {
        var answers = new List<EvaluationAnswerModel>
        {
            new()
            {
                QuestionId = questionId,
                AnswerId = answerId,
                AnswerValue = invalidAnswerValue
            }
        };

        var subject = CreateSubject();

        var ex = await Assert.ThrowsAnyAsync<AnswerValueFormatException>(async () =>
            await subject.Handle(CreateRequest(answers), default));

        Assert.Equal(questionId, ex.QuestionId);
        Assert.Equal(answerId, ex.AnswerId);
        Assert.Equal(invalidAnswerValue, ex.AnswerValue);
    }

    [Theory]
    [InlineData(100402)] // Cough up mucus
    [InlineData(100403)] // Chest sounds noisy
    [InlineData(100404)] // Shortness during physical
    public async Task Handle_WithInvalidFrequencyAnswerId_ThrowsUnsupportedAnswer(int questionId)
    {
        var answers = new List<EvaluationAnswerModel>
        {
            new()
            {
                QuestionId = questionId,
                AnswerId = 3,
                AnswerValue = "doesn't matter"
            }
        };

        var subject = CreateSubject();

        var ex = await Assert.ThrowsAnyAsync<UnsupportedAnswerForQuestionException>(async () =>
            await subject.Handle(CreateRequest(answers), default));

        Assert.Equal(questionId, ex.QuestionId);
        Assert.Equal(3, ex.AnswerId);
        Assert.Equal("doesn't matter", ex.AnswerValue);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public async Task Handle_DoesNotRequireEnvironmentQuestions_Tests(bool includeSymptom, bool includeEnv, bool includeComorbidity)
    {
        const string fvc = "25";
        const string fev1 = "125";
        const string fevOverFvc = "1.00";
        var answers = new List<EvaluationAnswerModel>
        {
            SpirometryTestPerformed,
            new() // Session Grade = C
            {
                QuestionId = 100297, AnswerId = 50939
            },
            new() // FVC
            {
                QuestionId = 100311, AnswerValue = fvc
            },
            new() // FEV1
            {
                QuestionId = 100312, AnswerValue = fev1
            },
            new() // FEV/FVC
            {
                QuestionId = 100298, AnswerValue = fevOverFvc
            }
            // No COPD diagnosis
        };

        if (includeSymptom)
        {
            answers.Add(new EvaluationAnswerModel // Symptom = Yes
            {
                QuestionId = 100299, AnswerId = 50944
            });
        }

        if (includeEnv)
        {
            answers.Add(new EvaluationAnswerModel // Env or Risk = Yes
            {
                QuestionId = 100300, AnswerId = 50947
            });
        }

        if (includeComorbidity)
        {
            answers.Add(new EvaluationAnswerModel // Comorbidity = Yes
            {
                QuestionId = 100301, AnswerId = 50950
            });
        }

        var expectedResult = new PerformedExamModel(EvaluationId,
            new RawExamResult
            {
                EvaluationId = EvaluationId,
                SessionGrade = SessionGrade.C,
                Fvc = fvc,
                Fev1 = fev1,
                Fev1FvcRatio = fevOverFvc,
                HasHighSymptom = includeSymptom ? TrileanType.Yes : null,
                HasEnvOrExpRisk = includeEnv ? TrileanType.Yes : null,
                HasHighComorbidity = includeComorbidity ? TrileanType.Yes : null,
                CopdDiagnosis = null
            });

        var subject = CreateSubject();

        var actual = await subject.Handle(CreateRequest(answers), default);

        Assert.Equal(expectedResult, actual);
    }

    [Theory]
    [InlineData("A", SessionGrade.A)]
    [InlineData("B", SessionGrade.B)]
    [InlineData("C", SessionGrade.C)]
    [InlineData("D", SessionGrade.D)]
    [InlineData("E", SessionGrade.E)]
    [InlineData("F", SessionGrade.F)]
    [InlineData("a", SessionGrade.A)]
    [InlineData("b", SessionGrade.B)]
    [InlineData("c", SessionGrade.C)]
    [InlineData("d", SessionGrade.D)]
    [InlineData("e", SessionGrade.E)]
    [InlineData("f", SessionGrade.F)]
    public async Task Handle_SessionGrade_NewerAnswerId_Test(string answerValue, SessionGrade expectedGrade)
    {
        // Arrange
        var answers = new List<EvaluationAnswerModel>
        {
            SpirometryTestPerformed,
            new EvaluationAnswerModel
            {
                QuestionId = 100297, AnswerId = 51947, AnswerValue = answerValue
            }
        };

        // Act
        var subject = CreateSubject();

        var actual = await subject.Handle(CreateRequest(answers), default);

        // Assert
        Assert.Equal(expectedGrade, actual.ExamResult.SessionGrade);
    }

    /// <summary>
    /// Test covering backwards-compatibility before SHRA-38516 has been completed (consolidating
    /// these two diagnosis history questions into one)
    /// </summary>
    [Theory]
    [MemberData(nameof(Handle_PreviousDiagnoses_TestData))]
    public async Task Handle_PreviousDiagnoses_v1_Tests(ICollection<string> chartReviewDiagnoses, ICollection<string> documentedAndAdditionalDiagnoses)
    {
        // Arrange
        var answers = new List<EvaluationAnswerModel>
        {
            SpirometryTestPerformed
        };

        answers.AddRange(chartReviewDiagnoses.Select(diagnosis =>
            new EvaluationAnswerModel {QuestionId = 89376, AnswerId = 29614, AnswerValue = diagnosis}));
        answers.AddRange(documentedAndAdditionalDiagnoses.Select(diagnosis =>
            new EvaluationAnswerModel {QuestionId = 85300, AnswerId = 21925, AnswerValue = diagnosis}));

        // Act
        var subject = CreateSubject();

        var actual = await subject.Handle(CreateRequest(answers), default);

        // Assert
        var allPreviousDiagnoses = new List<string>();
        allPreviousDiagnoses.AddRange(chartReviewDiagnoses);
        allPreviousDiagnoses.AddRange(documentedAndAdditionalDiagnoses);

        Assert.Equal(allPreviousDiagnoses.Count, actual.ExamResult.PreviousDiagnoses.Count);
        Assert.Empty(allPreviousDiagnoses.Except(actual.ExamResult.PreviousDiagnoses));
    }

    /// <summary>
    /// Test covering forwards-compatibility after SHRA-38516 has been completed (consolidating
    /// these two diagnosis history questions into one)
    /// </summary>
    [Theory]
    [MemberData(nameof(Handle_PreviousDiagnoses_TestData))]
    public async Task Handle_PreviousDiagnoses_v2_Tests(ICollection<string> chartReviewDiagnoses, ICollection<string> documentedAndAdditionalDiagnoses)
    {
        // Arrange
        var answers = new List<EvaluationAnswerModel>
        {
            SpirometryTestPerformed
        };

        void AddRange(IEnumerable<string> diagnoses)
            => answers.AddRange(diagnoses.Select(d => new EvaluationAnswerModel
            {
                QuestionId = 100496, AnswerId = 52027, AnswerValue = d
            }));

        AddRange(chartReviewDiagnoses);
        AddRange(documentedAndAdditionalDiagnoses);

        // Act
        var subject = CreateSubject();

        var actual = await subject.Handle(CreateRequest(answers), default);

        // Assert
        var allPreviousDiagnoses = new List<string>();
        allPreviousDiagnoses.AddRange(chartReviewDiagnoses);
        allPreviousDiagnoses.AddRange(documentedAndAdditionalDiagnoses);

        Assert.Equal(allPreviousDiagnoses.Count, actual.ExamResult.PreviousDiagnoses.Count);
        Assert.Empty(allPreviousDiagnoses.Except(actual.ExamResult.PreviousDiagnoses));
    }

    public static IEnumerable<object[]> Handle_PreviousDiagnoses_TestData()
    {
        yield return
        [
            new List<string>(),
            new List<string>()
        ];

        yield return
        [
            new[] {"diagnosis 1"},
            new[] {"diagnosis 1", "diagnosis 2", "diagnosis 3"}
        ];
    }

    /// <summary>
    /// Output CopdDiagnosis should be `true` if either Dx: COPD question is `true`
    /// </summary>
    [Theory]
    [InlineData(false, false, null)]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    public async Task Handle_CopdDiagnosis_Tests(bool assessmentCopdDiagnosis, bool heentCopdDiagnosis, bool? expectedResult)
    {
        // Arrange
        var answers = new List<EvaluationAnswerModel>
        {
            SpirometryTestPerformed
        };

        if (assessmentCopdDiagnosis)
        {
            answers.Add(new EvaluationAnswerModel
            {
                QuestionId = 100307, AnswerId = 50993
            });
        }

        if (heentCopdDiagnosis)
        {
            answers.Add(new EvaluationAnswerModel
            {
                QuestionId = 268, AnswerId = 20752
            });
        }

        // Act
        var subject = CreateSubject();

        var actual = await subject.Handle(CreateRequest(answers), default);

        // Assert
        Assert.Equal(expectedResult, actual.ExamResult.CopdDiagnosis);
    }

    [Theory]
    [InlineData(100307)] // Assessment section: "Dx: COPD"
    [InlineData(268)] // HEENT section: "Dx: COPD"
    public async Task Handle_WithInvalidCopdDiagnosisAnswerId_ThrowsUnsupportedAnswer(int questionId)
    {
        // Arrange
        const int invalidAnswerId = 1;
        const string answerValue = "unknown";

        var answers = new List<EvaluationAnswerModel>
        {
            SpirometryTestPerformed,
            new()
            {
                QuestionId = questionId,
                AnswerId = invalidAnswerId,
                AnswerValue = answerValue
            }
        };

        // Act/Assert
        var ex = await Assert.ThrowsAnyAsync<UnsupportedAnswerForQuestionException>(async () =>
            await CreateSubject().Handle(CreateRequest(answers), default));

        Assert.Equal(questionId, ex.QuestionId);
        Assert.Equal(invalidAnswerId, ex.AnswerId);
        Assert.Equal(answerValue, ex.AnswerValue);
    }
}