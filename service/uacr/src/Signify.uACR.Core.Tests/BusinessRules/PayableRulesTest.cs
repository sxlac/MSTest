using System;
using System.Collections.Generic;
using Signify.uACR.Core.BusinessRules;
using Signify.uACR.Core.Data.Entities;
using Signify.uACR.Core.Exceptions;
using Signify.uACR.Core.Models;
using Xunit;

namespace Signify.uACR.Core.Tests.BusinessRules;

public class PayableRulesTest
{
    // Subject under test
    private static BillAndPayRules CreateSubject() => new();

    [Theory]
    [MemberData(nameof(InvalidPayableInputCollection))]
    public void Should_Throw_When_Input_Invalid(PayableRuleAnswers payableRuleAnswers, string exception)
    {
        // Arrange
        var subject = CreateSubject();
        
        // Act
        var result = Assert.Throws<ArgumentException>(() => subject.IsPayable(payableRuleAnswers));
        
        // Assert
        Assert.Equal(exception, result.Message);
    }

    [Theory]
    [MemberData(nameof(LabResultCollection), "U", false, "Normality is Undetermined" )]
    [MemberData(nameof(LabResultCollection), "N", true, null)]
    [MemberData(nameof(LabResultCollection), "A", true, null)]
    public void Should_Validate_Normality_When_Results_Valid(PayableRuleAnswers payableRuleAnswers, bool isPayable, string reason)
    {
        // Arrange
        var subject = CreateSubject();
        
        // Act
        var result = subject.IsPayable(payableRuleAnswers);
        
        // Assert
        Assert.Equal(isPayable, result.IsMet);
        if (!isPayable)
        {
            Assert.NotNull(result.Reason);
            Assert.Equal(reason, result.Reason);
        }
    }
    
    [Fact]
    public void Should_Throw_When_Result_Data_Invalid()
    {
        // Arrange
        var payableRuleAnswers = new PayableRuleAnswers(123, Guid.NewGuid())
        {Result = new LabResult { Normality = "E" }};
        var subject = CreateSubject();
        
        // Act
        var result = Assert.Throws<UnableToDetermineBillabilityException>(() => subject.IsPayable(payableRuleAnswers));
        
        // Assert
        Assert.Contains("Insufficient information known about evaluation to determine billability", result.Message);
    }
    
    // Generate Test Data
    public static IEnumerable<object[]> InvalidPayableInputCollection()
    {
        yield return [null, "IsPayable should not be invoked with no BusinessRuleAnswers"];
        yield return [new PayableRuleAnswers(0, Guid.Empty), "IsPayable should not be invoked with empty Result"];
        yield return [new PayableRuleAnswers(1, Guid.Empty), "IsPayable should not be invoked with empty Result"];
        yield return [new PayableRuleAnswers(0, Guid.NewGuid()) , "IsPayable should not be invoked with empty Result"];
        yield return [new PayableRuleAnswers(0, Guid.NewGuid()) { Result = new LabResult() }, "IsPayable should not be invoked with default EvaluationId and EventId"
        ];
        yield return [new PayableRuleAnswers(1, Guid.Empty) { Result = new LabResult() }, "IsPayable should not be invoked with default EvaluationId and EventId"
        ];
    }
    
    public static IEnumerable<object[]> LabResultCollection(string normalityCode, bool result, string reason)
    {
        const long evaluationId = 123;
        var eventId = Guid.NewGuid();
        
        var answer = new PayableRuleAnswers(evaluationId, eventId)
        {
            Result = new LabResult
            {
                NormalityCode = normalityCode
            }
        };
        yield return [answer, result, reason];
    }
}