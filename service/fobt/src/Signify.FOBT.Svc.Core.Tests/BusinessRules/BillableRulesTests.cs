using Signify.FOBT.Svc.Core.BusinessRules;
using Signify.FOBT.Svc.Core.Data.Entities;
using Signify.FOBT.Svc.Core.Exceptions;
using Signify.FOBT.Svc.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.BusinessRules;

public class BillableRulesTests
{
    private static BillAndPayRules CreateSubject()
        => new();

    private static readonly IReadOnlySet<string> AllStates
        = "AL,AK,AZ,AR,CA,CO,CT,DE,DC,FL,GA,HI,ID,IL,IN,IA,KS,KY,LA,ME,MD,MA,MI,MN,MS,MO,MT,NE,NV,NH,NJ,NM,NY,NC,ND,OH,OK,OR,PA,RI,SC,SD,TN,TX,UT,VT,VA,WA,WV,WI,WY"
            .Split(',')
            .ToHashSet();

    [Theory]
    [MemberData(nameof(BillableRuleMetInAnswers))]
    public void Should_Return_True_When_Rules_Met(BillableRuleAnswers answers)
    {
        var subject = CreateSubject();
        var result = subject.IsBillableForResults(answers);
        Assert.True(result.IsMet);
        Assert.Null(result.Reason);
    }

    public static IEnumerable<object[]> BillableRuleMetInAnswers()
    {
        yield return
        [
            new BillableRuleAnswers
            {
                LabResults = new LabResults
                {
                    Exception = ""
                },
                Exam = new Core.Data.Entities.FOBT
                {
                    State = AllStates.First(),
                    EvaluationId = 1
                }
            }
        ];
        yield return
        [
            new BillableRuleAnswers
            {
                LabResults = new LabResults
                {
                    Exception = ""
                },
                Exam = new Core.Data.Entities.FOBT
                {
                    State = AllStates.First(),
                    EvaluationId = 1
                }
            }
        ];
    }

    [Theory]
    [MemberData(nameof(InvalidResults))]
    public void Should_Return_False_When_InvalidResults(BillableRuleAnswers answers)
    {
        var subject = CreateSubject();
        var result = subject.IsBillableForResults(answers);
        Assert.False(result.IsMet);
        Assert.Contains("Exam contains invalid lab results", result.Reason);
        Assert.DoesNotContain("State not billable", result.Reason);
    }

    public static IEnumerable<object[]> InvalidResults()
    {
        yield return
        [
            new BillableRuleAnswers
            {
                LabResults = new LabResults
                {
                    Exception = "Normality is not valid"
                },
                Exam = new Core.Data.Entities.FOBT
                {
                    State = AllStates.First(),
                    EvaluationId = 1
                }
            }
        ];
        yield return
        [
            new BillableRuleAnswers
            {
                LabResults = new LabResults
                {
                    Exception = "Normality is not valid"
                },
                Exam = new Core.Data.Entities.FOBT
                {
                    State = AllStates.First(),
                    EvaluationId = 1
                },
                IsValidLabResultsReceived = null
            }
        ];
        yield return
        [
            new BillableRuleAnswers
            {
                LabResults = null,
                IsValidLabResultsReceived = false,
                Exam = new Core.Data.Entities.FOBT
                {
                    State = AllStates.First(),
                    EvaluationId = 1
                }
            }
        ];
        yield return
        [
            new BillableRuleAnswers
            {
                IsValidLabResultsReceived = false,
                Exam = new Core.Data.Entities.FOBT
                {
                    State = AllStates.First(),
                    EvaluationId = 1
                }
            }
        ];
    }
    
    [Fact]
    public void Should_Throw_Exception_When_Invalid_Input()
    {
        var subject = CreateSubject();
        var answers = new BillableRuleAnswers();
        Assert.Throws<ArgumentException>(() => subject.IsBillableForResults(answers));
    }

    [Theory]
    [MemberData(nameof(BillableRuleAnswersStateNotSet))]
    public void Should_Throw_Exception_When_State_Not_Present(BillableRuleAnswers answers)
    {
        Assert.Throws<UnableToDetermineBillabilityException>(() => CreateSubject().IsBillableForResults(answers));
    }

    public static IEnumerable<object[]> BillableRuleAnswersStateNotSet()
    {
        yield return
        [
            new BillableRuleAnswers
            {
                Exam = new Core.Data.Entities.FOBT
                {
                    State = null,
                    EvaluationId = 1
                },
                LabResults = new LabResults
                {
                    Exception = ""
                }
            }
        ];
        yield return
        [
            new BillableRuleAnswers
            {
                Exam = new Core.Data.Entities.FOBT
                {
                    State = "",
                    EvaluationId = 1
                },
                LabResults = new LabResults
                {
                    Exception = ""
                }
            }
        ];
        yield return
        [
            new BillableRuleAnswers
            {
                Exam = new Core.Data.Entities.FOBT
                {
                    State = " ",
                    EvaluationId = 1
                },
                LabResults = new LabResults
                {
                    Exception = ""
                }
            }
        ];
    }

    [Theory]
    [MemberData(nameof(QualifiesBillingForResults_FullStateList_TestData))]
    public void Should_Return_True_When_State_Is_Billable(BillableRuleAnswers answers, bool expected)
    {
        var subject = CreateSubject();
        var result = subject.IsBillableForResults(answers);
        Assert.Equal(expected, result.IsMet);
        Assert.True(string.IsNullOrWhiteSpace(result.Reason));
    }

    public static IEnumerable<object[]> QualifiesBillingForResults_FullStateList_TestData()
    {
        foreach (var state in AllStates.Except(BillAndPayRules.ExcludedStates))
        {
            yield return
            [
                new BillableRuleAnswers
                {
                    Exam = new Core.Data.Entities.FOBT
                    {
                        State = state,
                        EvaluationId = 1
                    },
                    LabResults = new LabResults
                    {
                        Exception = ""
                    }
                },
                true
            ];
        }

        foreach (var state in AllStates.Except(BillAndPayRules.ExcludedStates))
        {
            yield return
            [
                new BillableRuleAnswers
                {
                    Exam = new Core.Data.Entities.FOBT
                    {
                        State = state.ToLower(),
                        EvaluationId = 1
                    },
                    LabResults = new LabResults
                    {
                        Exception = ""
                    }
                },
                true
            ];
        }

        foreach (var state in AllStates.Except(BillAndPayRules.ExcludedStates))
        {
            yield return
            [
                new BillableRuleAnswers
                {
                    Exam = new Core.Data.Entities.FOBT
                    {
                        State = state.ToUpper(),
                        EvaluationId = 1
                    },
                    LabResults = new LabResults
                    {
                        Exception = ""
                    }
                },
                true
            ];
        }
    }

    [Theory]
    [MemberData(nameof(QualifiesBillingForResults_NonBillableStateList_TestData))]
    public void Should_Return_False_When_Non_Billable_State(BillableRuleAnswers answers, bool expected, string expectedReason)
    {
        var subject = CreateSubject();
        var result = subject.IsBillableForResults(answers);
        Assert.Equal(expected, result.IsMet);
        Assert.Contains(expectedReason, result.Reason);
        Assert.DoesNotContain("Exam contains invalid lab results", result.Reason);
    }

    public static IEnumerable<object[]> QualifiesBillingForResults_NonBillableStateList_TestData()
    {
        foreach (var state in BillAndPayRules.ExcludedStates)
        {
            yield return
            [
                new BillableRuleAnswers
                {
                    Exam = new Core.Data.Entities.FOBT
                    {
                        State = state,
                        EvaluationId = 1
                    },
                    LabResults = new LabResults
                    {
                        Exception = ""
                    }
                },
                false,
                $"Exam performed in a state that cannot be billed for: {state}"
            ];
        }
    }

    [Theory]
    [MemberData(nameof(BillableRuleAnswersTestLeftBehindStateNotPopulated))]
    public void Should_Return_False_When_Test_LeftBehind_State_Not_Billable(BillableRuleAnswers answers)
    {
        Assert.Throws<UnableToDetermineBillabilityException>(() => CreateSubject().IsBillableForResults(answers));
    }

    public static IEnumerable<object[]> BillableRuleAnswersTestLeftBehindStateNotPopulated()
    {
        yield return
        [
            new BillableRuleAnswers
            {
                Exam = new Core.Data.Entities.FOBT
                {
                    State = "",
                    EvaluationId = 1
                }
            }
        ];
        yield return
        [
            new BillableRuleAnswers
            {
                Exam = new Core.Data.Entities.FOBT
                {
                    State = " ",
                    EvaluationId = 1
                },
            }
        ];
        yield return
        [
            new BillableRuleAnswers
            {
                Exam = new Core.Data.Entities.FOBT
                {
                    State = null,
                    EvaluationId = 1
                },
            }
        ];
    }

    [Theory]
    [MemberData(nameof(BillableRuleAnswersMultipleFailures))]
    public void Should_Return_False_With_Reasons_When_Multiple_Rules_Not_Met(BillableRuleAnswers answers)
    {
        var subject = CreateSubject();
        var result = subject.IsBillableForResults(answers);
        Assert.False(result.IsMet);
        Assert.False(string.IsNullOrWhiteSpace(result.Reason));
        Assert.Contains("Exam performed in a state that cannot be billed for", result.Reason);
        Assert.Contains("Exam contains invalid lab results", result.Reason);
    }

    public static IEnumerable<object[]> BillableRuleAnswersMultipleFailures()
    {
        yield return
        [
            new BillableRuleAnswers
            {
                LabResults = new LabResults
                {
                    Exception = "Failure"
                },
                Exam = new Core.Data.Entities.FOBT
                {
                    State = BillAndPayRules.ExcludedStates.First(),
                    EvaluationId = 1
                }
            }
        ];
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("  ", true)]
    [InlineData("anything else", false)]
    public void AreValidResults_Tests(string exception, bool expected)
    {
        var subject = CreateSubject();
        var billableRules = new BillableRuleAnswers
        {
            LabResults = new LabResults
            {
                Exception = exception
            }
        };

        var actual = subject.IsLabResultValid(billableRules);

        Assert.Equal(expected, actual.IsMet);
    }
}