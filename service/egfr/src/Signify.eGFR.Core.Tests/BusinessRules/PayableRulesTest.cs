using System;
using System.Collections.Generic;
using Signify.eGFR.Core.BusinessRules;
using Signify.eGFR.Core.Models;
using Xunit;

namespace Signify.eGFR.Core.Tests.BusinessRules;

public class PayableRulesTest
{
    private static BillAndPayRules CreateSubject()
        => new();

    [Theory]
    [MemberData(nameof(InvalidInputCollection))]
    public void Should_Throw_when_Inputs_Invalid(PayableRuleAnswers answers)
    {
        var subject = CreateSubject();
        Assert.Throws<ArgumentException>(() => subject.IsPayable(answers));
    }

    [Theory]
    [MemberData(nameof(LabResultCollection), "U", false, "Normality is Undetermined")]
    [MemberData(nameof(LabResultCollection), "N", true, null)]
    [MemberData(nameof(LabResultCollection), "A", true, null)]
    public void Should_Validate_Normality(PayableRuleAnswers answers, bool isPayable, string reason)
    {
        var subject = CreateSubject();
        var result = subject.IsPayable(answers);
        Assert.Equal(isPayable, result.IsMet);
        if (!isPayable)
        {
            Assert.NotNull(result.Reason);
            Assert.Equal(reason, result.Reason);
        }
    }

    public static IEnumerable<object[]> LabResultCollection(string normalityCode, bool result, string reason)
    {
        const long evaluationId = 123;
        var eventId = Guid.NewGuid();
        var answer = new PayableRuleAnswers(evaluationId, eventId)
        {
            NormalityCode = normalityCode
        };
        yield return [answer, result, reason];
    }

    public static IEnumerable<object[]> InvalidInputCollection()
    {
        yield return
        [
            new PayableRuleAnswers(0, Guid.Empty)
        ];
        yield return
        [
            new PayableRuleAnswers(1, Guid.Empty)
        ];
        yield return
        [
            new PayableRuleAnswers(0, Guid.NewGuid())
        ];
        yield return
        [
            new PayableRuleAnswers(0, Guid.NewGuid())
            {
                NormalityCode = ""
            }
        ];
        yield return
        [
            new PayableRuleAnswers(1, Guid.Empty)
            {
                NormalityCode = null
            }
        ];
        yield return
        [
            null
        ];
    }
}