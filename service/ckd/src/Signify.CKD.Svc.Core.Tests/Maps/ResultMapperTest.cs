using System;
using System.Collections.Generic;
using System.Linq;
using FakeItEasy;
using Signify.CKD.Svc.Core.BusinessRules;
using Signify.CKD.Svc.Core.Maps;
using Signify.CKD.Svc.Core.Models;
using Xunit;
using Result = Signify.CKD.Svc.Core.Messages.Result;
using CKDEntity = Signify.CKD.Svc.Core.Data.Entities.CKD;
using LookUpCKDAnswer = Signify.CKD.Svc.Core.Data.Entities.LookupCKDAnswer;

namespace Signify.CKD.Svc.Core.Tests.Maps;

public class ResultMapperTests
{
    private readonly IBillableRules _billableRules = A.Fake<IBillableRules>();
    private ResultsMapper CreateSubject() => new(_billableRules);

    [Fact]
    public void Ckd_Data_Mapper_Check_ValidResult()
    {
        //Arrange
        var ckd = new CKDEntity
        {
            EvaluationId = 351523,
            ExpirationDate = DateTime.Now,
            CKDAnswer = "Albumin: 10 - Creatinine: 0.1 ; Cannot be determined"
        };
        var destination = new Result();
        var subject = CreateSubject();

        A.CallTo(() => _billableRules.IsBillable(A<BillableRuleAnswers>._)).Returns(new BusinessRuleStatus{IsMet = true});

        //Act
        var actual = subject.Convert(ckd, destination, default);

        //Assert
        Assert.DoesNotContain(actual.Results, item => item.Type == "Exception" && item.Result == "Invalid Strip Result");
        Assert.DoesNotContain(actual.Results, item => item.Type == "Exception" && item.Result == "Invalid Expiry Date");
        Assert.Equal("CKD", actual.ProductCode);
        Assert.Equal(ckd.EvaluationId, actual.EvaluationId);
        Assert.True(actual.IsBillable);
        Assert.Equal(ckd.ExpirationDate?.Date, actual.ExpiryDate?.Date);
    }

    [Fact]
    public void Ckd_Data_Mapper_Check_InValidResult()
    {
        //Arrange
        var ckd = new CKDEntity
        {
            EvaluationId = 351524,
            ExpirationDate = DateTime.Now,
            CKDAnswer = string.Empty
        };
        var destination = new Result();
        var subject = CreateSubject();

        A.CallTo(() => _billableRules.IsBillable(A<BillableRuleAnswers>._)).Returns(new BusinessRuleStatus{IsMet = false});

        //Act
        var actual = subject.Convert(ckd, destination, default);

        //Assert
        Assert.Contains(actual.Results, item => item.Type == "Exception" && item.Result == "Invalid Strip Result");
        Assert.DoesNotContain(actual.Results, item => item.Type == "Exception" && item.Result == "Invalid Expiry Date");
        Assert.Equal("CKD", actual.ProductCode);
        Assert.Equal(ckd.EvaluationId, actual.EvaluationId);
        Assert.False(actual.IsBillable);
        Assert.Equal(ckd.ExpirationDate?.Date, actual.ExpiryDate?.Date);
    }

    [Fact]
    public void Ckd_Data_Mapper_Check_InValidExpiryDate()
    {
        //Arrange
        var ckd = new CKDEntity
        {
            EvaluationId = 351524,
            ExpirationDate = null,
            CKDAnswer = "Albumin: 10 - Creatinine: 0.1 ; Cannot be determined"
        };
        var destination = new Result();
        var subject = CreateSubject();

        A.CallTo(() => _billableRules.IsBillable(A<BillableRuleAnswers>._)).Returns(new BusinessRuleStatus{IsMet = true});

        //Act
        var actual = subject.Convert(ckd, destination, default);

        //Assert
        Assert.DoesNotContain(actual.Results, item => item.Type == "Exception" && item.Result == "Invalid Strip Result");
        Assert.Contains(actual.Results, item => item.Type == "Exception" && item.Result == "Invalid Expiry Date");
        Assert.Equal(ckd.EvaluationId, actual.EvaluationId);
        Assert.True(actual.IsBillable);
        Assert.Equal(ckd.ExpirationDate?.Date, actual.ExpiryDate?.Date);
    }

    [Fact]
    public void Ckd_Data_Mapper_Check_PerformedDateAndReceivedDate()
    {
        //Arrange
        var ckd = new CKDEntity
        {
            EvaluationId = 351523,
            ReceivedDateTime = DateTime.UtcNow.AddDays(-5),
            CreatedDateTime = DateTime.UtcNow.AddDays(-2),
        };
        var destination = new Result();
        var subject = CreateSubject();

        A.CallTo(() => _billableRules.IsBillable(A<BillableRuleAnswers>._)).Returns(new BusinessRuleStatus { IsMet = true });

        //Act
        var actual = subject.Convert(ckd, destination, default);

        //Assert
        Assert.Equal(ckd.CreatedDateTime.Date, actual.PerformedDate?.Date);
        Assert.Equal(ckd.ReceivedDateTime.Date, actual.ReceivedDate.Date);
    }

    [Fact]
    public void Ckd_Data_Mapper_Check_InValidExpiryDate_InvalidResult()
    {
        //Arrange
        var ckd = new CKDEntity
        {
            EvaluationId = 351524,
            CreatedDateTime = DateTime.Now,
            ReceivedDateTime = DateTime.Now,
            ExpirationDate = null,
            CKDAnswer = string.Empty
        };
        var destination = new Result();
        var subject = CreateSubject();

        A.CallTo(() => _billableRules.IsBillable(A<BillableRuleAnswers>._)).Returns(new BusinessRuleStatus{IsMet = false});

        //Act
        var actual = subject.Convert(ckd, destination, default);

        //Assert
        Assert.Contains(actual.Results, item => item.Type == "Exception" && item.Result == "Invalid Strip Result");
        Assert.Contains(actual.Results, item => item.Type == "Exception" && item.Result == "Invalid Expiry Date");
        Assert.Equal(ckd.EvaluationId, actual.EvaluationId);
        Assert.False(actual.IsBillable);
        Assert.Equal(ckd.CreatedDateTime.Date, actual.PerformedDate?.Date);
        Assert.Equal(ckd.ReceivedDateTime.Date, actual.ReceivedDate.Date);
        Assert.Equal(ckd.ExpirationDate?.Date, actual.ExpiryDate?.Date);
    }

    [Theory]
    [MemberData(nameof(GetLookupCkdAnswers))]
    public void Overall_Normality_Mapper_Check_Scenarios(string normality, int albuminValue, decimal creatinineValue, string uAcr, string severity)
    {
        //Arrange
        var lookUpCkdEntity = new LookUpCKDAnswer
        {
            NormalityIndicator = normality,
            Albumin = albuminValue,
            Creatinine = creatinineValue,
            Acr = uAcr,
            Severity = severity,
            CKDAnswerValue = $"Albumin: {albuminValue} - Creatinine: {creatinineValue} ; {normality}"
        };
        var destination = new Result();
        var subject = CreateSubject();

        //Act
        var actual = subject.Convert(lookUpCkdEntity, destination, default);

        var albuminResult = actual.Results.First(item => item.Type == "Albumin");
        var creatinineResult = actual.Results.First(item => item.Type == "Creatinine");
        var acrResult = actual.Results.First(item => item.Type == "uAcr");

        var actualAlbuminValue = albuminResult.Result;
        var actualCreatinineValue = creatinineResult.Result;
        var actualAcrValue = acrResult.Result;
        var actualAlbuminUnit = albuminResult.ResultUnit;
        var actualCreatinineUnit = creatinineResult.ResultUnit;
        var actualAcrUnit = acrResult.ResultUnit;
        var actualAcrSeverity = acrResult.Severity;

        //Assert
        Assert.Equal(lookUpCkdEntity.Albumin.ToString(), actualAlbuminValue);
        Assert.Equal(lookUpCkdEntity.Creatinine.ToString(), actualCreatinineValue);
        Assert.Equal(lookUpCkdEntity.Acr, actualAcrValue);
        Assert.Equal("mg/L", actualAlbuminUnit);
        Assert.Equal("g/L", actualCreatinineUnit);
        Assert.Equal("mg/g", actualAcrUnit);
        Assert.Equal(lookUpCkdEntity.NormalityIndicator, actual.Determination);
        Assert.Equal(lookUpCkdEntity.Severity, actualAcrSeverity);
    }

    [Fact]
    public void Overall_Normality_Mapper_Undetermined()
    {
        //Arrange
        var lookUpCkdEntity = new LookUpCKDAnswer();
        var destination = new Result();
        var subject = CreateSubject();

        //Act
        var actual = subject.Convert(lookUpCkdEntity, destination, default);

        //Assert
        Assert.Equal(Constants.Application.NormalityIndicator.Undetermined,actual.Determination);
    }

    public static IEnumerable<object[]> GetLookupCkdAnswers()
    {
        yield return new object[]
        {
            "U",
            10,
            0.1,
            null,
            null
        };

        yield return new object[]
        {
            "A",
            30,
            0.1,
            "30-300mg/g",
            null

        };

        yield return new object[]
        {
            "A",
            80,
            0.1,
            ">300mg/g",
            "High"
        };

        yield return new object[]
        {
            "A",
            150,
            0.1,
            ">300mg/g",
            "High"
        };

        yield return new object[]
        {
            "N",
            10,
            0.5,
            "<30mg/g",
            null
        };

        yield return new object[]
        {
            "A",
            30,
            0.5,
            "30-300mg/g",
            null
        };

        yield return new object[]
        {
            "A",
            80,
            0.5,
            "30-300mg/g",
            null
        };

        yield return new object[]
        {
            "A",
            150,
            0.5,
            "30-300mg/g",
            null
        };

        yield return new object[]
        {
            "N",
            10,
            1.0,
            "<30mg/g",
            null
        };

        yield return new object[]
        {
            "A",
            30,
            1.0,
            "30-300mg/g",
            null
        };

        yield return new object[]
        {
            "A",
            80,
            1.0,
            "30-300mg/g",
            null
        };

        yield return new object[]
        {
            "A",
            150,
            1.0,
            "30-300mg/g",
            null
        };

        yield return new object[]
        {
            "N",
            10,
            2.0,
            "<30mg/g",
            null
        };

        yield return new object[]
        {
            "N",
            30,
            2.0,
            "<30mg/g",
            null
        };

        yield return new object[]
        {
            "A",
            80,
            2.0,
            "30-300mg/g",
            null
        };

        yield return new object[]
        {
            "A",
            150,
            2.0,
            "30-300mg/g",
            null
        };

        yield return new object[]
        {
            "A",
            10,
            3.0,
            "<30mg/g",
            null
        };
        yield return new object[]
        {
            "N",
            30,
            3.0,
            "<30mg/g",
            null
        };
        yield return new object[]
        {
            "N",
            80,
            3.0,
            "<30mg/g",
            null
        };
        yield return new object[]
        {
            "A",
            150,
            3.0,
            "30-300mg/g",
            null
        };
    }
}
