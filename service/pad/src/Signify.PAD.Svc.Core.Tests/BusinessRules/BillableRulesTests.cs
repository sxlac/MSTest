using Signify.PAD.Svc.Core.BusinessRules;
using Signify.PAD.Svc.Core.Constants;
using Signify.PAD.Svc.Core.Models;
using System.Collections.Generic;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.BusinessRules;

public class BillableRulesTests
{
    private static BillAndPayRules CreateSubject()
        => new();

    [Theory]
    [MemberData(nameof(AnswerCollectionAtLeastOneSideIs), "A")]
    [MemberData(nameof(AnswerCollectionAtLeastOneSideIs), "N")]
    public void Should_Return_True_When_Rules_Are_Met(BillableRuleAnswers answers)
    {
        var subject = CreateSubject();
        var result = subject.IsBillable(answers);
        Assert.True(result.IsMet);
        Assert.Null(result.Reason);
    }

    [Theory]
    [MemberData(nameof(BillableRuleAnswerUndeterminedCollection))]
    public void Should_Return_True_When_Rules_Are_Not_Met(BillableRuleAnswers answers)
    {
        var subject = CreateSubject();
        var result = subject.IsBillable(answers);
        Assert.False(result.IsMet);
        Assert.False(string.IsNullOrWhiteSpace(result.Reason));
    }

    [Theory]
    [MemberData(nameof(AnswerCollectionAtLeastOneSideIs), "N")]
    public void Should_Return_True_If_Is_Normal(BillableRuleAnswers answers)
    {
        var subject = CreateSubject();
        var result = subject.IsNormal(answers);
        Assert.True(result);
    }

    [Theory]
    [MemberData(nameof(AnswerCollectionNeitherSideIs), "N")]
    public void Should_Return_False_If_Not_Normal(BillableRuleAnswers answers)
    {
        var subject = CreateSubject();
        var result = subject.IsNormal(answers);
        Assert.False(result);
    }

    [Theory]
    [MemberData(nameof(AnswerCollectionAtLeastOneSideIs), "A")]
    public void Should_Return_True_If_Is_Abnormal(BillableRuleAnswers answers)
    {
        var subject = CreateSubject();
        var result = subject.IsAbnormal(answers);
        Assert.True(result);
    }

    [Theory]
    [MemberData(nameof(AnswerCollectionNeitherSideIs), "A")]
    public void Should_Return_False_If_Not_Abnormal(BillableRuleAnswers answers)
    {
        var subject = CreateSubject();
        var result = subject.IsAbnormal(answers);
        Assert.False(result);
    }

    public static IEnumerable<object[]> BillableRuleAnswerUndeterminedCollection()
    {
        yield return
        [
            new BillableRuleAnswers
            {
                LeftNormalityIndicator = Application.NormalityIndicator.Undetermined,
                RightNormalityIndicator = Application.NormalityIndicator.Undetermined
            }
        ];
    }

    public static IEnumerable<object[]> AnswerCollectionAtLeastOneSideIs(string indicator)
    {
        foreach (var answer in All)
        {
            if (answer.LeftNormalityIndicator == indicator || answer.RightNormalityIndicator == indicator)
            {
                yield return [answer];
            }
        }
    }
    
    public static IEnumerable<object[]> AnswerCollectionNeitherSideIs(string indicator)
    {
        foreach (var answer in All)
        {
            if (!(answer.LeftNormalityIndicator == indicator || answer.RightNormalityIndicator == indicator))
            {
                yield return [answer];
            }
        }
    }

    private static readonly List<BillableRuleAnswers> All =
    [
        new BillableRuleAnswers
        {
            LeftNormalityIndicator = Application.NormalityIndicator.Abnormal,
            RightNormalityIndicator = Application.NormalityIndicator.Abnormal
        },
        new BillableRuleAnswers
        {
            LeftNormalityIndicator = Application.NormalityIndicator.Abnormal,
            RightNormalityIndicator = Application.NormalityIndicator.Normal
        },
        new BillableRuleAnswers
        {
            LeftNormalityIndicator = Application.NormalityIndicator.Abnormal,
            RightNormalityIndicator = Application.NormalityIndicator.Undetermined
        },
        new BillableRuleAnswers
        {
            LeftNormalityIndicator = Application.NormalityIndicator.Normal,
            RightNormalityIndicator = Application.NormalityIndicator.Abnormal
        },
        new BillableRuleAnswers
        {
            LeftNormalityIndicator = Application.NormalityIndicator.Undetermined,
            RightNormalityIndicator = Application.NormalityIndicator.Abnormal
        },
        new BillableRuleAnswers
        {
            LeftNormalityIndicator = Application.NormalityIndicator.Normal,
            RightNormalityIndicator = Application.NormalityIndicator.Normal
        },
        new BillableRuleAnswers
        {
            LeftNormalityIndicator = Application.NormalityIndicator.Normal,
            RightNormalityIndicator = Application.NormalityIndicator.Undetermined
        },
        new BillableRuleAnswers
        {
            LeftNormalityIndicator = Application.NormalityIndicator.Undetermined,
            RightNormalityIndicator = Application.NormalityIndicator.Normal
        },
        new BillableRuleAnswers
        {
            LeftNormalityIndicator = Application.NormalityIndicator.Undetermined,
            RightNormalityIndicator = Application.NormalityIndicator.Undetermined
        }
    ];
}