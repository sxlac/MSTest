using System.Collections.Generic;
using Iris.Public.Types.Models;
using Signify.DEE.Svc.Core.BusinessRules;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Messages.Models;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.BusinessRules;

public class BillAndPayRulesTest
{
    private static BillAndPayRules CreateSubject()
        => new();

    #region Billing

    [Theory]
    [MemberData(nameof(Results_TestData), 1, 1, true, true, false, true)]
    [MemberData(nameof(Results_TestData), 1, 1, true, false, false, true)]
    [MemberData(nameof(Results_TestData), 1, 1, false, true, false, true)]
    [MemberData(nameof(Results_TestData), 1, 1, false, false, true, false)]
    [MemberData(nameof(Results_TestData), 1, 1, false, false, false, false)]
    [MemberData(nameof(Results_TestData), 1, 0, true, true, false, false)]
    [MemberData(nameof(Results_TestData), 1, 0, true, true, true, true)]
    [MemberData(nameof(Results_TestData), 0, 1, true, true, true, true)]
    [MemberData(nameof(Results_TestData), 0, 1, true, true, false, false)]
    [MemberData(nameof(Results_TestData), 0, 0, true, true, false, false)]
    [MemberData(nameof(Results_TestData), 0, 0, false, false, false, false)]
    [MemberData(nameof(Results_TestData), 0, 0, false, false, true, false)]
    public void Determine_Billability_When_Gradings_Available(ResultImageDetails imageDetails, ResultGrading gradings, bool hasEnucleation, bool expectedResult)
    {
        var answers = new BillableRuleAnswers
        {
            Gradings = gradings,
            ImageDetails = imageDetails,
            HasEnucleation = hasEnucleation
        };
        var subject = CreateSubject();

        var result = subject.IsBillable(answers);

        Assert.Equal(expectedResult, result.IsMet);
        if (expectedResult)
        {
            Assert.Null(result.Reason);
        }
        else
        {
            Assert.NotNull(result.Reason);
        }
    }

    [Theory]
    [MemberData(nameof(Gradings_TestData), true, true, true)]
    [MemberData(nameof(Gradings_TestData), true, false, true)]
    [MemberData(nameof(Gradings_TestData), false, true, true)]
    [MemberData(nameof(Gradings_TestData), false, false, false)]
    public void Determine_Gradings_When_Gradings_Available(ResultGrading gradings, bool expectedResult)
    {
        var subject = CreateSubject();

        var result = subject.IsGradable(new BillableRuleAnswers { Gradings = gradings });

        Assert.Equal(expectedResult, result.IsMet);
        if (expectedResult)
        {
            Assert.Null(result.Reason);
        }
        else
        {
            Assert.NotNull(result.Reason);
        }
    }

    #endregion

    #region Payment

    [Theory]
    [MemberData(nameof(Results_TestData), 1, 1, true, true, false, true)]
    [MemberData(nameof(Results_TestData), 1, 1, true, false, false, true)]
    [MemberData(nameof(Results_TestData), 1, 1, true, false, true, true)]
    [MemberData(nameof(Results_TestData), 1, 1, false, true, false, true)]
    [MemberData(nameof(Results_TestData), 1, 1, false, false, false, false)]
    [MemberData(nameof(Results_TestData), 1, 0, true, true, false, false)]
    [MemberData(nameof(Results_TestData), 1, 0, true, true, true, true)]
    [MemberData(nameof(Results_TestData), 0, 1, true, true, false, false)]
    [MemberData(nameof(Results_TestData), 0, 1, true, false, true, true)]
    [MemberData(nameof(Results_TestData), 0, 0, true, true, false, false)]
    [MemberData(nameof(Results_TestData), 0, 0, true, true, true, false)]
    [MemberData(nameof(Results_TestData), 0, 0, false, false, false, false)]
    public void Determine_Payability_When_Gradings_Available(ResultImageDetails imageDetails, ResultGrading gradings, bool hasEnucleation, bool expectedResult)
    {
        var answers = new PayableRuleAnswers
        {
            Gradings = gradings,
            ImageDetails = imageDetails,
            HasEnucleation = hasEnucleation
        };
        var subject = CreateSubject();

        var result = subject.IsPayable(answers);

        Assert.Equal(expectedResult, result.IsMet);
        if (expectedResult)
        {
            Assert.Null(result.Reason);
        }
        else
        {
            Assert.NotNull(result.Reason);
        }
    }

    [Fact]
    public void Determine_Payability_When_Gradings_And_StatusCodes_Not_Available()
    {
        var answers = new PayableRuleAnswers();
        var subject = CreateSubject();

        var result = subject.IsPayable(answers);

        Assert.False(result.IsMet);

        Assert.NotNull(result.Reason);
        Assert.Equal("Invalid data", result.Reason);
    }

    [Theory]
    [MemberData(nameof(Results_ExamStatus_TestData))]
    public void Determine_Payability_When_StatusCodes_Available(List<int> statusCodes, bool expectedResult)
    {
        var answers = new PayableRuleAnswers
        {
            StatusCodes = statusCodes
        };
        var subject = CreateSubject();

        var result = subject.IsPayable(answers);

        Assert.Equal(expectedResult, result.IsMet);
        if (expectedResult)
        {
            Assert.Null(result.Reason);
        }
        else
        {
            Assert.NotNull(result.Reason);
        }
    }

    #endregion

    #region Helpers

    private static ResultGrading CreateGrading(bool hasLeftEyeFinding, bool hasRightEyeFinding)
    {
        var leftEyeFindingList = new List<ResultFinding>();
        var rightEyeFindingList = new List<ResultFinding>();

        if (hasLeftEyeFinding) leftEyeFindingList.Add(new ResultFinding { Finding = "Diabetic Retinopathy", Result = "None" });
        if (hasRightEyeFinding) rightEyeFindingList.Add(new ResultFinding { Finding = "Diabetic Retinopathy", Result = "None" });

        var gradings = new ResultGrading
        {
            OD = new ResultEyeSideGrading
            {
                Findings = leftEyeFindingList
            },
            OS = new ResultEyeSideGrading
            {
                Findings = rightEyeFindingList
            }
        };

        return gradings;
    }

    private static ResultImageDetails CreateImageDetails(int leftEyeOriginalCount, int rightEyeOriginalCount)
    {
        return new ResultImageDetails
        {
            LeftEyeOriginalCount = leftEyeOriginalCount,
            RightEyeOriginalCount = rightEyeOriginalCount
        };
    }

    /// <summary>
    /// Creates samples for Billing and Payment rules check based on ResultGrading and ResultImageDetails
    /// </summary>
    /// <returns>ResultGrading, ResultImageDetails, ExpectedResult</returns>
    public static IEnumerable<object[]> Results_TestData(int leftEyeOriginalCount, int rightEyeOriginalCount, bool hasLeftEyeFinding,
        bool hasRightEyeFinding, bool hasEnucleation, bool expectedResult)
    {
        yield return new object[]
        {
            CreateImageDetails(leftEyeOriginalCount, rightEyeOriginalCount), CreateGrading(hasLeftEyeFinding, hasRightEyeFinding), hasEnucleation, expectedResult
        };
    }

    /// <summary>
    /// Creates samples for Billing and Payment rules check based on ExamStatus table data
    /// </summary>
    /// <returns>StatusCode list as ints, ExpectedResult</returns>
    public static IEnumerable<object[]> Results_ExamStatus_TestData()
    {
        yield return new object[]
        {
            new List<int>
            {
                (int)ExamStatusCode.StatusCodes.Gradable,
                (int)ExamStatusCode.StatusCodes.Performed,
                (int)ExamStatusCode.StatusCodes.Incomplete
            },
            false
        };
        yield return new object[]
        {
            new List<int>
            {
                (int)ExamStatusCode.StatusCodes.Gradable,
                (int)ExamStatusCode.StatusCodes.Performed
            },
            true
        };
        yield return new object[]
        {
            new List<int>
            {
                (int)ExamStatusCode.StatusCodes.NotGradable,
                (int)ExamStatusCode.StatusCodes.Performed,
                (int)ExamStatusCode.StatusCodes.Incomplete
            },
            false
        };
        yield return new object[]
        {
            new List<int>
            {
                (int)ExamStatusCode.StatusCodes.NotGradable,
                (int)ExamStatusCode.StatusCodes.Performed
            },
            false
        };
    }

    /// <summary>
    /// Creates samples for Gradings rules check based on Gradings data
    /// </summary>
    /// <returns>ResultGrading, ExpectedResult</returns>
    public static IEnumerable<object[]> Gradings_TestData(bool hasLeftEyeFinding, bool hasRightEyeFinding, bool expectedResult)
    {
        yield return new object[]
        {
            CreateGrading(hasLeftEyeFinding, hasRightEyeFinding), expectedResult
        };
    }

    #endregion
}