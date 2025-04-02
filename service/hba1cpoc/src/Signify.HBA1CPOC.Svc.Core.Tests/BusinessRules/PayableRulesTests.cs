using Signify.HBA1CPOC.Svc.Core.BusinessRules;
using Signify.HBA1CPOC.Svc.Core.Models;
using System.Collections.Generic;
using System;
using Xunit;

namespace Signify.HBA1CPOC.Svc.Core.Tests.BusinessRules;

public class PayableRulesTests
{
    private static BillAndPayRules CreateSubject()
        => new();

    [Theory]
    [MemberData(nameof(PayableRuleAnswersValid))]
    public void Should_Return_True_When_RulesAre_Met(PayableRuleAnswers answers)
    {
        var subject = CreateSubject();
        var result = subject.IsPayable(answers);
        Assert.True(result.IsMet);
        Assert.Null(result.Reason);
    }

    [Theory]
    [MemberData(nameof(InvalidExpirationDate))]
    [MemberData(nameof(InvalidDateOfService))]
    [MemberData(nameof(ExpirationDateLessThanDateOfService))]
    [MemberData(nameof(UndeterminedNormality))]
    public void Should_Return_False_When_RulesAre_NotMet(PayableRuleAnswers answers)
    {
        var subject = CreateSubject();
        var result = subject.IsPayable(answers);
        Assert.False(result.IsMet);
        Assert.False(string.IsNullOrWhiteSpace(result.Reason));
    }

    [Theory]
    [MemberData(nameof(UndeterminedNormalityAndInvalidDates))]
    public void Should_Return_False_When_RulesAre_NotMet_MultipleFailReason(PayableRuleAnswers answers)
    {
        var subject = CreateSubject();
        var result = subject.IsPayable(answers);
        Assert.False(result.IsMet);
        Assert.False(string.IsNullOrWhiteSpace(result.Reason));
        Assert.Contains(",", result.Reason);
    }

    public static IEnumerable<object[]> PayableRuleAnswersValid()
    {
        var now = DateTime.UtcNow;
        yield return
        [
            new PayableRuleAnswers
            {
                ExpirationDate = DateOnly.FromDateTime(now.AddYears(1)),
                DateOfService = now,
                NormalityIndicator = Normality.Normal
            }
        ];
        yield return
        [
            new PayableRuleAnswers
            {
                ExpirationDate = DateOnly.FromDateTime(now),
                DateOfService = now,
                NormalityIndicator = Normality.Normal
            }
        ];
        yield return
        [
            new PayableRuleAnswers
            {
                ExpirationDate = DateOnly.FromDateTime(now.AddYears(1)),
                DateOfService = now,
                NormalityIndicator = Normality.Abnormal
            }
        ];
        yield return
        [
            new PayableRuleAnswers
            {
                ExpirationDate = DateOnly.FromDateTime(now),
                DateOfService = now,
                NormalityIndicator = Normality.Abnormal
            }
        ];
    }

    public static IEnumerable<object[]> InvalidExpirationDate()
    {
        var now = DateTime.UtcNow;
        yield return
        [
            new PayableRuleAnswers
            {
                DateOfService = now,
                NormalityIndicator = Normality.Normal
            }
        ];
        yield return
        [
            new PayableRuleAnswers
            {
                ExpirationDate = DateOnly.FromDateTime(new DateTime()),
                DateOfService = now,
                NormalityIndicator = Normality.Normal
            }
        ];
    }

    public static IEnumerable<object[]> InvalidDateOfService()
    {
        var now = DateTime.UtcNow;
        yield return
        [
            new PayableRuleAnswers
            {
                ExpirationDate = DateOnly.FromDateTime(now),
                NormalityIndicator = Normality.Normal
            }
        ];
        yield return
        [
            new PayableRuleAnswers
            {
                ExpirationDate = DateOnly.FromDateTime(now),
                DateOfService = new DateTime(),
                NormalityIndicator = Normality.Normal
            }
        ];
    }

    public static IEnumerable<object[]> ExpirationDateLessThanDateOfService()
    {
        var now = DateTime.UtcNow;
        yield return
        [
            new PayableRuleAnswers
            {
                ExpirationDate = DateOnly.FromDateTime(now.AddMonths(-1)),
                DateOfService = now,
                NormalityIndicator = Normality.Normal
            }
        ];
    }

    public static IEnumerable<object[]> UndeterminedNormality()
    {
        var now = DateTime.UtcNow;
        yield return
        [
            new PayableRuleAnswers
            {
                ExpirationDate = DateOnly.FromDateTime(now.AddMonths(1)),
                DateOfService = now
            }
        ];
        yield return
        [
            new PayableRuleAnswers
            {
                ExpirationDate = DateOnly.FromDateTime(now.AddMonths(1)),
                DateOfService = now,
                NormalityIndicator = Normality.Undetermined
            }
        ];
    }

    public static IEnumerable<object[]> UndeterminedNormalityAndInvalidDates()
    {
        var now = DateTime.UtcNow;
        yield return
        [
            new PayableRuleAnswers
            {
                ExpirationDate = DateOnly.FromDateTime(now.AddMonths(-1)),
                DateOfService = now,
                NormalityIndicator = Normality.Undetermined
            }
        ];
        yield return
        [
            new PayableRuleAnswers
            {
                ExpirationDate = DateOnly.FromDateTime(now),
                DateOfService = new DateTime(),
                NormalityIndicator = Normality.Undetermined
            }
        ];
        yield return
        [
            new PayableRuleAnswers
            {
                ExpirationDate = DateOnly.FromDateTime(new DateTime()),
                DateOfService = now,
                NormalityIndicator = Normality.Undetermined
            }
        ];
    }
}