using System;
using System.Collections.Generic;
using Signify.uACR.Core.BusinessRules;
using Signify.uACR.Core.Data.Entities;
using Signify.uACR.Core.Exceptions;
using Signify.uACR.Core.Models;
using Xunit;

namespace Signify.uACR.Core.Tests.BusinessRules;

public class BillableRulesTest 
{
    // Subject under test
    private static BillAndPayRules CreateSubject() => new();

    [Theory]
    [MemberData(nameof(InvalidBillableInputCollection))]
    public void Should_Throw_When_Input_Invalid(BillableRuleAnswers billableRuleAnswers, string exception)
    {
        // Arrange
        var subject = CreateSubject();
        
        // Act
        var result = Assert.Throws<ArgumentException>(() => subject.IsBillable(billableRuleAnswers));
        
        // Assert
        Assert.Equal(exception, result.Message);
    }
    
    [Theory]
    [MemberData(nameof(LabResultCollection), "U", false, "Normality is Undetermined" )]
    [MemberData(nameof(LabResultCollection), "N", true, null)]
    [MemberData(nameof(LabResultCollection), "A", true, null)]
    public void Should_Validate_Normality_When_Results_Valid(BillableRuleAnswers billableRuleAnswers, bool isPayable, string reason)
    {
        // Arrange
        var subject = CreateSubject();
        
        // Act
        var result = subject.IsBillable(billableRuleAnswers);
        
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
        var billableRuleAnswers = new BillableRuleAnswers(123, Guid.NewGuid())
            {Result = new LabResult { Normality = "E" }};
        var subject = CreateSubject();
        
        // Act
        var result = Assert.Throws<UnableToDetermineBillabilityException>(() => subject.IsBillable(billableRuleAnswers));
        
        // Assert
        Assert.Contains("Insufficient information known about evaluation to determine billability", result.Message);
    }
    
    // Generate Test Data
    public static IEnumerable<object[]> InvalidBillableInputCollection()
    {
        yield return [null, "IsBillable should not be invoked with no BusinessRuleAnswers"];
        yield return [new BillableRuleAnswers(0, Guid.Empty), "IsBillable should not be invoked with empty Result"];
        yield return [new BillableRuleAnswers(1, Guid.Empty), "IsBillable should not be invoked with empty Result"];
        yield return [new BillableRuleAnswers(0, Guid.NewGuid()) , "IsBillable should not be invoked with empty Result"
        ];
        yield return [new BillableRuleAnswers(0, Guid.NewGuid()) { Result = new LabResult() }, "IsBillable should not be invoked with default EvaluationId and EventId"
        ];
        yield return [new BillableRuleAnswers(1, Guid.Empty) { Result = new LabResult() }, "IsBillable should not be invoked with default EvaluationId and EventId"
        ];
    }
    
    public static IEnumerable<object[]> LabResultCollection(string normalityCode, bool result, string reason)
    {
        const long evaluationId = 123;
        var eventId = Guid.NewGuid();
        
        var answer = new BillableRuleAnswers(evaluationId, eventId)
        {
            Result = new LabResult
            {
                NormalityCode = normalityCode
            }
        };
        yield return [answer, result, reason];
    }
}