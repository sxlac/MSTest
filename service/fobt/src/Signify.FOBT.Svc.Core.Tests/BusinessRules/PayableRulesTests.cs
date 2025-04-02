using Signify.FOBT.Svc.Core.BusinessRules;
using Signify.FOBT.Svc.Core.Data.Entities;
using Signify.FOBT.Svc.Core.Models;
using System.Collections.Generic;
using System;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.BusinessRules;

public class PayableRulesTests
{
    private static BillAndPayRules CreateSubject()
        => new();

    [Theory]
    [MemberData(nameof(PayableRuleMet))]
    public void Should_Return_True_When_Rules_Met(PayableRuleAnswers answers)
    {
        var subject = CreateSubject();
        var result = subject.IsPayable(answers);
        Assert.True(result.IsMet);
        Assert.Null(result.Reason);
    }

    public static IEnumerable<object[]> PayableRuleMet()
    {
        yield return
        [
            new PayableRuleAnswers
            {
                IsValidLabResultsReceived = true
            }
        ];
        yield return
        [
            new PayableRuleAnswers
            {
                LabResults = new LabResults
                {
                    Exception = ""
                }
            }
        ];
        yield return
        [
            new PayableRuleAnswers
            {
                LabResults = null,
                IsValidLabResultsReceived = true
            }
        ];
        yield return
        [
            new PayableRuleAnswers
            {
                LabResults = new LabResults
                {
                    Exception = ""
                },
                IsValidLabResultsReceived = null
            }
        ];
    }

    [Theory]
    [MemberData(nameof(PayableRuleNotMet))]
    public void Should_Return_False_When_Rules_Not_Met(PayableRuleAnswers answers, String expectedReason)
    {
        var subject = CreateSubject();
        var result = subject.IsPayable(answers);
        Assert.False(result.IsMet);
        Assert.Contains(expectedReason, result.Reason);
    }

    public static IEnumerable<object[]> PayableRuleNotMet()
    {
        yield return
        [
            new PayableRuleAnswers
            {
                LabResults = new LabResults
                {
                    Exception = "Normality is not valid"
                }
            },
            "Exam contains invalid lab results"
        ];
        yield return
        [
            new PayableRuleAnswers
            {
                IsValidLabResultsReceived = false
            },
            "Exam contains invalid lab results"
        ];
    }

    [Fact]
    public void Should_Throw_When_Invalid_Input()
    {
        var subject = CreateSubject();
        var answers = new PayableRuleAnswers();
        Assert.ThrowsAny<ArgumentException>(() => subject.IsPayable(answers));
    }
}