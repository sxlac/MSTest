using Signify.PAD.Svc.Core.BusinessRules;
using System.Collections.Generic;
using Signify.PAD.Svc.Core.Constants;
using Signify.PAD.Svc.Core.Models;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.BusinessRules;

public class PayableRulesTests
{
    private static BillAndPayRules CreateSubject()
        => new();

    [Theory]
    [MemberData(nameof(PayableRuleAnswerAbnormalCollection))]
    [MemberData(nameof(PayableRuleAnswerNormalCollection))]
    public void Should_Return_True_When_RulesAre_Met(PayableRuleAnswers answers)
    {
        var subject = CreateSubject();
        var result = subject.IsPayable(answers);
        Assert.True(result.IsMet);
        Assert.Null(result.Reason);
    }

    [Theory]
    [MemberData(nameof(PayableRuleAnswerUndeterminedCollection))]
    public void Should_Return_True_When_RulesAre_NotMet(PayableRuleAnswers answers)
    {
        var subject = CreateSubject();
        var result = subject.IsPayable(answers);
        Assert.False(result.IsMet);
        Assert.False(string.IsNullOrWhiteSpace(result.Reason));
    }

    public static IEnumerable<object[]> PayableRuleAnswerAbnormalCollection()
    {
        yield return
        [
            new PayableRuleAnswers
            {
                LeftNormalityIndicator = Application.NormalityIndicator.Abnormal,
                RightNormalityIndicator = Application.NormalityIndicator.Abnormal
            }
        ];
        yield return
        [
            new PayableRuleAnswers
            {
                LeftNormalityIndicator = Application.NormalityIndicator.Abnormal,
                RightNormalityIndicator = Application.NormalityIndicator.Normal
            }
        ];
        yield return
        [
            new PayableRuleAnswers
            {
                LeftNormalityIndicator = Application.NormalityIndicator.Abnormal,
                RightNormalityIndicator = Application.NormalityIndicator.Undetermined
            }
        ];
        yield return
        [
            new PayableRuleAnswers
            {
                LeftNormalityIndicator = Application.NormalityIndicator.Normal,
                RightNormalityIndicator = Application.NormalityIndicator.Abnormal
            }
        ];
        yield return
        [
            new PayableRuleAnswers
            {
                LeftNormalityIndicator = Application.NormalityIndicator.Undetermined,
                RightNormalityIndicator = Application.NormalityIndicator.Abnormal
            }
        ];
    }

    public static IEnumerable<object[]> PayableRuleAnswerNormalCollection()
    {
        yield return
        [
            new PayableRuleAnswers
            {
                LeftNormalityIndicator = Application.NormalityIndicator.Normal,
                RightNormalityIndicator = Application.NormalityIndicator.Normal
            }
        ];
        yield return
        [
            new PayableRuleAnswers
            {
                LeftNormalityIndicator = Application.NormalityIndicator.Normal,
                RightNormalityIndicator = Application.NormalityIndicator.Undetermined
            }
        ];

        yield return
        [
            new PayableRuleAnswers
            {
                LeftNormalityIndicator = Application.NormalityIndicator.Undetermined,
                RightNormalityIndicator = Application.NormalityIndicator.Normal
            }
        ];
    }

    public static IEnumerable<object[]> PayableRuleAnswerUndeterminedCollection()
    {
        yield return
        [
            new PayableRuleAnswers
            {
                LeftNormalityIndicator = Application.NormalityIndicator.Undetermined,
                RightNormalityIndicator = Application.NormalityIndicator.Undetermined
            }
        ];
    }
}