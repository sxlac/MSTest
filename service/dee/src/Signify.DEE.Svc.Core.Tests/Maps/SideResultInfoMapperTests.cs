using FakeItEasy;
using Signify.DEE.Messages;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Maps;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Maps;

public class SideResultInfoMapperTests
{
    private readonly IOverallNormalityMapper _overallNormalityMapper = A.Fake<IOverallNormalityMapper>();
    private static readonly int Left = LateralityCode.Left.LateralityCodeId;
    private static readonly int Right = LateralityCode.Right.LateralityCodeId;

    private SideResultInfoMapper CreateSubject()
        => new SideResultInfoMapper(_overallNormalityMapper);

    [Fact]
    public void Convert_ReturnsSameInstanceAsUpdated()
    {
        var destination = new List<SideResultInfo>();

        var subject = CreateSubject();

        var actual = subject.Convert(ResultMapperTests.BuildEmptySource(), destination, null);

        Assert.Equal(destination, actual);
    }

    [Fact]
    public void Convert_SingleLateralityWithImage_CompletesSuccessfully()
    {
        var destination = new List<SideResultInfo>();
        var subject = CreateSubject();

        var actual = subject.Convert(ResultMapperTests.BuildEmptySourceLeftLateralityOnlyWithImage(), destination, null);
        Assert.Equal(destination, actual);
    }

    [Fact]
    public void Convert_WithNullDestination_ReturnsNotNull()
    {
        var subject = CreateSubject();

        var actual = subject.Convert(ResultMapperTests.BuildEmptySource(), null, null);

        Assert.NotNull(actual);
    }

    [Fact]
    public void Convert_WithNoData_StillReturnsBothSides()
    {
        var subject = CreateSubject();

        var actual = subject.Convert(ResultMapperTests.BuildEmptySource(), null, null);

        Assert.Equal(2, actual.Count);
        Assert.Single(actual, each => each.Side == "L");
        Assert.Single(actual, each => each.Side == "R");
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(null, false)]
    [InlineData(false, null)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Convert_SetsPathology(bool? leftPathology, bool? rightPathology)
    {
        var source = ResultMapperTests.BuildEmptySource();

        source.ExamResults.First().LeftEyeHasPathology = leftPathology;
        source.ExamResults.First().RightEyeHasPathology = rightPathology;

        var subject = CreateSubject();

        var actual = subject.Convert(source, null, null);

        Assert.Equal(leftPathology, actual.First(each => each.Side == "L").Pathology);
        Assert.Equal(rightPathology, actual.First(each => each.Side == "R").Pathology);
    }

    #region Findings
    private class SideFindingComparer : IEqualityComparer<SideFinding>
    {
        public bool Equals(SideFinding x, SideFinding y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.Finding == y.Finding && x.Result == y.Result && x.AbnormalIndicator == y.AbnormalIndicator;
        }

        public int GetHashCode(SideFinding obj)
        {
            return HashCode.Combine(obj.Finding, obj.Result, obj.AbnormalIndicator);
        }
    }

    [Theory]
    [MemberData(nameof(Convert_Findings_TestData))]
    public void Convert_Findings_Tests(
        IEnumerable<ExamFinding> sourceFindings,
        ICollection<SideFinding> expectedLeftFindings,
        ICollection<SideFinding> expectedRightFindings)
    {
        // Arrange
        var source = ResultMapperTests.BuildEmptySource();

        var examResult = source.ExamResults.First();

        foreach (var finding in sourceFindings)
        {
            examResult.ExamFindings.Add(finding);
        }

        // Act
        var subject = CreateSubject();

        var sideResults = subject.Convert(source, null, null);

        // Assert
        var actualLeft = sideResults.First(each => each.Side == "L");
        var actualRight = sideResults.First(each => each.Side == "R");

        var comparer = new SideFindingComparer();

        // Verify findings are what we expect
        EnumerableComparer.AssertComparable(expectedLeftFindings, actualLeft.Findings, comparer);
        EnumerableComparer.AssertComparable(expectedRightFindings, actualRight.Findings, comparer);

        // Verify call to get overall normality is as we expect
        A.CallTo(() => _overallNormalityMapper.GetOverallNormality(A<IEnumerable<string>>.That.Matches(e =>
                e.Count() == actualLeft.Findings.Count)))
            .MustHaveHappened();
        A.CallTo(() => _overallNormalityMapper.GetOverallNormality(A<IEnumerable<string>>.That.Matches(e =>
                e.Count() == actualRight.Findings.Count)))
            .MustHaveHappened();
    }

    public static IEnumerable<object[]> Convert_Findings_TestData()
    {
        const string normal = "N";
        const string abnormal = "A";
        const string undetermined = "U";

        int left = LateralityCode.Left.LateralityCodeId;
        int right = LateralityCode.Right.LateralityCodeId;

        yield return new object[]
        {
            new List<ExamFinding>
            {
                new ExamFinding { LateralityCodeId = left, NormalityIndicator = normal, Finding = "Diabetic Retinopathy - Mild" }
            },
            new List<SideFinding> // Left
            {
                new SideFinding { AbnormalIndicator = normal, Finding = "Diabetic Retinopathy", Result = "Mild" }
            },
            new List<SideFinding>() // Right
        };

        yield return new object[]
        {
            new List<ExamFinding>
            {
                new ExamFinding { LateralityCodeId = right, NormalityIndicator = undetermined, Finding = "Macular Edema - Positive" }
            },
            new List<SideFinding>(), // Left
            new List<SideFinding> // Right
            {
                new SideFinding { AbnormalIndicator = undetermined, Finding = "Macular Edema", Result = "Positive" }
            }
        };

        yield return new object[]
        {
            new List<ExamFinding>
            {
                new ExamFinding { LateralityCodeId = left, NormalityIndicator = abnormal, Finding = "Macular Edema - Positive" },
                new ExamFinding { LateralityCodeId = right, NormalityIndicator = normal, Finding = "Other - Other" }
            },
            new List<SideFinding> // Left
            {
                new SideFinding { AbnormalIndicator = abnormal, Finding = "Macular Edema", Result = "Positive" }
            },
            new List<SideFinding> // Right
            {
                new SideFinding { AbnormalIndicator = normal, Finding = "Other", Result = "Other" }
            }
        };

        yield return new object[]
        {
            new List<ExamFinding>
            {
                new ExamFinding { LateralityCodeId = left, NormalityIndicator = abnormal, Finding = "Macular Edema - Positive" },
                new ExamFinding { LateralityCodeId = left, NormalityIndicator = undetermined, Finding = "Macular Edema - None" },
                new ExamFinding { LateralityCodeId = right, NormalityIndicator = normal, Finding = "Other - Other" }
            },
            new List<SideFinding> // Left
            {
                new SideFinding { AbnormalIndicator = undetermined, Finding = "Macular Edema", Result = "None" },
                new SideFinding { AbnormalIndicator = abnormal, Finding = "Macular Edema", Result = "Positive" } // order in the list doesn't matter
            },
            new List<SideFinding> // Right
            {
                new SideFinding { AbnormalIndicator = normal, Finding = "Other", Result = "Other" }
            }
        };

        #region Edge cases
        yield return new object[]
        {
            new List<ExamFinding>
            {
                new ExamFinding { LateralityCodeId = left, NormalityIndicator = abnormal, Finding = "Macular Edema - Positive" },
                new ExamFinding { LateralityCodeId = left, NormalityIndicator = undetermined, Finding = "Macular Edema - None" },
                new ExamFinding { LateralityCodeId = 0, NormalityIndicator = normal, Finding = "Other - Other" } // Invalid laterality
            },
            new List<SideFinding> // Left
            {
                new SideFinding { AbnormalIndicator = undetermined, Finding = "Macular Edema", Result = "None" },
                new SideFinding { AbnormalIndicator = abnormal, Finding = "Macular Edema", Result = "Positive" } // Order in the list doesn't matter
            },
            new List<SideFinding>() // Right
        };

        yield return new object[]
        {
            new List<ExamFinding>
            {
                new ExamFinding { LateralityCodeId = left, NormalityIndicator = abnormal, Finding = "Macular Edema-Positive" }, // Spaces missing before/after separator
                new ExamFinding { LateralityCodeId = right, NormalityIndicator = normal, Finding = "Macular Edema    -    None" } // Too many spaces before/after separator
            },
            new List<SideFinding> // Left
            {
                new SideFinding { AbnormalIndicator = abnormal, Finding = "Macular Edema", Result = "Positive" }
            },
            new List<SideFinding> // Right
            {
                new SideFinding { AbnormalIndicator = normal, Finding = "Macular Edema", Result = "None" }
            }
        };
        #endregion Edge cases

    }
    #endregion Findings

    #region Grade & Not Gradable Reasons
    [Theory]
    [MemberData(nameof(Convert_Grade_NotGradableReasons_TestData))]
    public void Convert_Grade_NotGradableReasons_Tests(
        bool sourceLeftEyeGrade,
        bool sourceRightEyeGrade,
        ICollection<NonGradableReason> sourceLeftEyeReasons,
        ICollection<NonGradableReason> sourceRightEyeReasons,
        ICollection<string> expectedLeftNotGradableReasons,
        ICollection<string> expectedRightNotGradableReasons)
    {
        // Arrange
        var source = ResultMapperTests.BuildEmptySource();

        var examGradeLeft = source.ExamLateralityGrades.First(lg => lg.LateralityCodeId == Left);

        examGradeLeft.Gradable = sourceLeftEyeGrade;

        foreach (var ngr in sourceLeftEyeReasons)
        {
            examGradeLeft.NonGradableReasons.Add(ngr);
        }

        var examGradeRight = source.ExamLateralityGrades.First(lg => lg.LateralityCodeId == Right);

        examGradeRight.Gradable = sourceRightEyeGrade;

        foreach (var ngr in sourceRightEyeReasons)
        {
            examGradeRight.NonGradableReasons.Add(ngr);
        }

        // Act
        var subject = CreateSubject();

        var sideResults = subject.Convert(source, null, null);

        // Assert
        var actualLeft = sideResults.First(each => each.Side == "L");
        var actualRight = sideResults.First(each => each.Side == "R");

        //Verify findings are what we expect
        EnumerableComparer.AssertComparable(expectedLeftNotGradableReasons, actualLeft.NotGradableReasons, StringComparer.InvariantCulture);
        EnumerableComparer.AssertComparable(expectedRightNotGradableReasons, actualRight.NotGradableReasons, StringComparer.InvariantCulture);

        Assert.Equal(sourceLeftEyeGrade, actualLeft.Gradable);
        Assert.Equal(sourceRightEyeGrade, actualRight.Gradable);
    }

    public static IEnumerable<object[]> Convert_Grade_NotGradableReasons_TestData()
    {
        int left = LateralityCode.Left.LateralityCodeId;
        int right = LateralityCode.Right.LateralityCodeId;

        yield return new object[]
        {
            true,//left
            true,//right
            new List<NonGradableReason>(),//left
            new List<NonGradableReason>(), // right
            new List<string>(),
            new List<string>()
        };

        yield return new object[]
        {
            false,//left
            true,//right
            new List<NonGradableReason>()
            {
                new NonGradableReason()
                {
                    Reason = "Image not of a retina"
                }
            },
            new List<NonGradableReason>(), // right
            new List<string>()
            {
                "Image not of a retina"
            },
            new List<string>()
        };

        yield return new object[]
        {
            false,//left
            true,//right
            new List<NonGradableReason>()
            {
                new NonGradableReason()//Duplicates.
                {
                    Reason = "Image not of a retina"
                },
                new NonGradableReason()
                {
                    Reason = "Image not of a retina"
                }
            },
            new List<NonGradableReason>(), // right
            new List<string>()
            {
                "Image not of a retina"
            },
            new List<string>()
        };

        yield return new object[]
        {
            false,//left
            false,//right
            new List<NonGradableReason>()//left
            {
                new NonGradableReason()
                {
                    Reason = "Image not of a retina"
                }
            },
            new List<NonGradableReason>()//right
            {
                new NonGradableReason()//Duplicates.
                {
                    Reason = "Image not of a retina"
                }
            },
            new List<string>()//left
            {
                "Image not of a retina"
            },
            new List<string>()//right
            {
                "Image not of a retina"
            },
        };

        yield return new object[]
        {
            true,//left
            false,//right
            new List<NonGradableReason>(),//left
            new List<NonGradableReason>()//right
            {
                new NonGradableReason()//Duplicates.
                {
                    Reason = "Image not of a retina"
                },
                new NonGradableReason()//Duplicates.
                {
                    Reason = "Media Opacity"
                }
            },
            new List<string>(),//Left
            new List<string>()//right
            {
                "Image not of a retina",
                "Media Opacity"
            },
        };

        yield return new object[]
        {
            true,//left
            false,//right
            new List<NonGradableReason>(),//left
            new List<NonGradableReason>()//right
            {
                new NonGradableReason()//Duplicates.
                {
                    Reason = "Image not of a retina"
                },
                new NonGradableReason()//Duplicates.
                {
                    Reason = "Media Opacity"
                },
                new NonGradableReason()//Duplicates.
                {
                    Reason = "Image not of a retina"
                },
            },
            new List<string>(),//Left
            new List<string>()//right
            {
                "Image not of a retina",
                "Media Opacity"
            },
        };

        yield return new object[]
        {
            true,//left
            false,//right
            new List<NonGradableReason>(),//left
            new List<NonGradableReason>()//right
            {
                new NonGradableReason()//Duplicates.
                {
                    Reason = "Media Opacity"
                },
                new NonGradableReason()//Duplicates.
                {
                    Reason = "Media Opacity"
                },
            },
            new List<string>(),//Left
            new List<string>()//right
            {
                "Media Opacity"
            },
        };

        #region Edge cases
        yield return new object[]
        {
            true,//left
            false,//right
            new List<NonGradableReason>(),//left
            new List<NonGradableReason>()//right
            {
                new NonGradableReason()//Duplicates.
                {
                    Reason = "  ",
                },
                new NonGradableReason()//Duplicates.
                {
                    Reason = ""
                },
                new NonGradableReason()//Duplicates.
                {
                    Reason = null
                },
            },
            new List<string>(),//Left
            new List<string>()//right
        };
        #endregion Edge cases
    }
    #endregion Grade & Not Gradable Reasons
}