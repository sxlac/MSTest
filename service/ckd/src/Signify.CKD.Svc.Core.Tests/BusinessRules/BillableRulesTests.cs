using System;
using Signify.CKD.Svc.Core.BusinessRules;
using Signify.CKD.Svc.Core.Models;
using Xunit;

namespace Signify.CKD.Svc.Core.Tests.BusinessRules;

public class BillableRulesTests
{
    private static BillAndPayRules CreateSubject()
        => new();

    [Theory]
    [InlineData( "2023-01-01", "2023-02-01", true)]
    [InlineData( "2023-02-01", "2023-01-01", false)]
    public void IsPayable_DateOfService_MustBeBefore_ExpirationDate(DateTime dateOfService, DateTime expirationDate, bool expectedResult)
    {
        // Arrange
        var answers = new PayableRuleAnswers
        {
            ExpirationDate = expirationDate,
            DateOfService = dateOfService,
            CkdAnswer = "Albumin: 10 - Creatinine: 2.0 ; Normal",
            IsPerformed = true
        };
        var rules = CreateSubject();

        // Act
        var result = rules.IsPayable(answers);

        // Assert
        Assert.Equal(expectedResult, result.IsMet);
    }
    
    [Theory]
    [InlineData( "Albumin: 10 - Creatinine: 2.0 ; Normal", true)]
    [InlineData( "", false)]
    [InlineData( null, false)]
    public void IsPayable_CkdAnswer_MustBeValid(string ckdAnswer, bool expectedResult)
    {
        // Arrange
        var answers = new PayableRuleAnswers
        {
            ExpirationDate = new DateTime(2023, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            DateOfService = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CkdAnswer = ckdAnswer,
            IsPerformed = true
        };
        var rules = CreateSubject();

        // Act
        var result = rules.IsPayable(answers);

        // Assert
        Assert.Equal(expectedResult, result.IsMet);
    }
    
    [Theory]
    [InlineData( true, true)]
    [InlineData( false, false)]
    public void IsPayable_IsPerformed_MustBeValid(bool isPerformed, bool expectedResult)
    {
        // Arrange
        var answers = new PayableRuleAnswers
        {
            ExpirationDate = new DateTime(2023, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            DateOfService = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CkdAnswer = "Albumin: 10 - Creatinine: 2.0 ; Normal",
            IsPerformed = isPerformed
        };
        var rules = CreateSubject();

        // Act
        var result = rules.IsPayable(answers);

        // Assert
        Assert.Equal(expectedResult, result.IsMet);
    }
    
    [Theory]
    [InlineData( "Albumin: 10 - Creatinine: 2.0 ; Normal", true)]
    [InlineData( "", false)]
    [InlineData( null, false)]
    public void IsBillable_ckdAnswer_MustBeValid(string ckdAnswer, bool expectedResult)
    {
        // Arrange
        var answers = new BillableRuleAnswers()
        {
            CkdAnswer = ckdAnswer
        };
        var rules = CreateSubject();

        // Act
        var result = rules.IsBillable(answers);

        // Assert
        Assert.Equal(expectedResult, result.IsMet);
    }
}