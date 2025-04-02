using Signify.HBA1CPOC.Svc.Core.BusinessRules;
using Signify.HBA1CPOC.Svc.Core.Models;
using System.Collections.Generic;
using Xunit;

namespace Signify.HBA1CPOC.Svc.Core.Tests.BusinessRules;

public class BillableRulesTests
{
    private static BillAndPayRules CreateSubject()
        => new();

    [Theory]
    [MemberData(nameof(BillableRuleAnswersNormalityNormalAbnormal))]
    public void Should_Return_True_When_RulesAre_Met(BillableRuleAnswers answers)
    {
        var subject = CreateSubject();
        var result = subject.IsBillable(answers);
        Assert.True(result.IsMet);
        Assert.Null(result.Reason);
    }

    [Theory]
    [MemberData(nameof(BillableRuleAnswersNormalityUndetermined))]
    public void Should_Return_False_When_RulesAre_NotMet(BillableRuleAnswers answers)
    {
        var subject = CreateSubject();
        var result = subject.IsBillable(answers);
        Assert.False(result.IsMet);
        Assert.False(string.IsNullOrWhiteSpace(result.Reason));
    }

    public static IEnumerable<object[]> BillableRuleAnswersNormalityNormalAbnormal()
    {
        yield return
        [
            new BillableRuleAnswers
            {
                NormalityIndicator = Normality.Normal
            }
        ];
        yield return
        [
            new BillableRuleAnswers
            {
                NormalityIndicator = Normality.Abnormal
            }
        ];
    }

    public static IEnumerable<object[]> BillableRuleAnswersNormalityUndetermined()
    {
        yield return
        [
            new BillableRuleAnswers()
        ];
        yield return
        [
            new BillableRuleAnswers
            {
                NormalityIndicator = Normality.Undetermined
            }
        ];
    }
}