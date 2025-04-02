using AutoMapper;
using FakeItEasy;
using Signify.DEE.Messages;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Maps;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Maps;

public class ResultMapperTests
{
    private const string SideNormalityMapperResult = "N";
    private static readonly int Left = LateralityCode.Left.LateralityCodeId;
    private static readonly int Right = LateralityCode.Right.LateralityCodeId;
    private readonly IOverallNormalityMapper _subjectsNormalityMapper = A.Fake<IOverallNormalityMapper>();
    private readonly IOverallNormalityMapper _sideResultMappersNormalityMapper = A.Fake<IOverallNormalityMapper>();
    private readonly FakeApplicationTime _applicationTime = new();

    public ResultMapperTests()
    {
        A.CallTo(() => _sideResultMappersNormalityMapper.GetOverallNormality(A<IEnumerable<string>>._))
            .Returns(SideNormalityMapperResult); // Just always have this mapper return the same thing
    }

    private IMapper CreateSubject()
    {
        // Cannot unit test the ResultsReceivedMapper directly in this case, since it needs the ResolutionContext,
        // which is not an interface so it cannot be mocked. Need to test using mapper.Map() instead.
        // https://github.com/AutoMapper/AutoMapper/discussions/3726

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(
                new MappingProfile());
            cfg.ConstructServicesUsing(ResolveService);
        });
        return config.CreateMapper();
    }

    private object ResolveService(Type type)
    {
        if (type == typeof(ResultMapper))
            return new ResultMapper(_subjectsNormalityMapper, _applicationTime);

        if (type == typeof(SideResultInfoMapper))
            return new SideResultInfoMapper(_sideResultMappersNormalityMapper);

        return null;
    }

    internal static Exam BuildEmptySource()
        => new Exam
        {
            EvaluationId = 1,
            ExamResults = new List<ExamResult>
            {
                new ExamResult
                {
                    ExamDiagnoses = new List<ExamDiagnosis>()
                }
            },
            ExamLateralityGrades = new List<ExamLateralityGrade>
            {
                new ExamLateralityGrade
                {
                    LateralityCodeId  = Right,
                    NonGradableReasons = new List<NonGradableReason>()
                },
                new ExamLateralityGrade
                {
                    LateralityCodeId  = Left,
                    NonGradableReasons = new List<NonGradableReason>()
                },
            },
        };

    internal static Exam BuildEmptySourceLeftLateralityOnlyWithImage()
        => new Exam
        {
            EvaluationId = 1,
            ExamResults = new List<ExamResult>
            {
                new ExamResult
                {
                    ExamDiagnoses = new List<ExamDiagnosis>()
                }
            },
            ExamLateralityGrades = new List<ExamLateralityGrade>
            {

                new ExamLateralityGrade
                {
                    LateralityCodeId  = Left,
                    NonGradableReasons = new List<NonGradableReason>()
                },
            },
            ExamImages = new List<ExamImage>()
            {
                new ExamImage()
                {
                    LateralityCodeId = Left
                }
            }
        };

    private static bool CollectionMatches<T>(IEnumerable<T> actual, IEnumerable<T> expected, IEqualityComparer<T> comparer)
    {
        EnumerableComparer.AssertComparable(actual, expected, comparer); // Assert will throw if false

        return true;
    }

    [Fact]
    public void Convert_ReturnsSameInstanceAsUpdated()
    {
        var destination = new Result();

        var subject = CreateSubject();

        var actual = subject.Map(BuildEmptySource(), destination);

        Assert.Equal(destination, actual);
    }

    [Fact]
    public void Convert_SetsCustomNotGradableReason_WhenNoImagesProvided()
    {
        var destination = new Result();

        var subject = CreateSubject();

        var actual = subject.Map(BuildEmptySource(), destination);

        Assert.Equal("No images are available", actual.Results.First().NotGradableReasons.First());
    }

    [Fact]
    public void Convert_WithNullDestination_ReturnsNotNull()
    {
        var subject = CreateSubject();

        var actual = subject.Map(BuildEmptySource(), (Result)null);

        Assert.NotNull(actual);
    }

    [Fact]
    public void Convert_HappyPath()
    {
        // Arrange
        var resultDownloadedDateTime = new DateTime(2022, 01, 02);
        var dateSigned = new DateTimeOffset(DateTime.UtcNow, TimeSpan.Zero);

        var source = new Exam
        {
            EvaluationId = 1,
            CreatedDateTime = new DateTime(2022, 01, 01),
            ExamResults = new List<ExamResult>
            {
                new ExamResult
                {
                    CarePlan = "care plan",
                    GraderFirstName = "first name",
                    GraderLastName = "last name",
                    DateSigned = dateSigned,
                    GraderNpi = "npi",
                    GraderTaxonomy = "taxonomy",
                    ExamDiagnoses = new List<ExamDiagnosis>(),
                    NormalityIndicator = "N"
                }
            },
            ExamStatuses = new List<ExamStatus>
            {
                new ExamStatus
                {
                    ExamStatusCodeId = ExamStatusCode.ResultDataDownloaded.ExamStatusCodeId,
                    ReceivedDateTime = resultDownloadedDateTime
                }
            }
        };

        // Act
        var subject = CreateSubject();

        var actual = subject.Map<Result>(source);

        Assert.Equal("DEE", actual.ProductCode);
        Assert.Equal(1, actual.EvaluationId);
        Assert.Equal(source.CreatedDateTime, actual.PerformedDate);
        Assert.Equal(resultDownloadedDateTime, actual.ReceivedDate);
        Assert.Equal(dateSigned, actual.DateGraded);
        Assert.Equal("care plan", actual.CarePlan);

        Assert.Equal("first name", actual.Grader.FirstName);
        Assert.Equal("last name", actual.Grader.LastName);
        Assert.Equal("npi", actual.Grader.NPI);
        Assert.Equal("taxonomy", actual.Grader.Taxonomy);
        Assert.Equal("N", actual.Determination);

        Assert.Empty(actual.DiagnosisCodes);

        Assert.NotNull(actual.Results);       
    }

    [Fact]
    public void Map_CreateDateTime_To_PerformedDate()
    {
        // Arrange
        var resultDownloadedDateTime = new DateTime(2022, 01, 02);

        var source = new Exam
        {
            EvaluationId = 1,
            CreatedDateTime = new DateTime(2022, 01, 01),
            ExamResults = new List<ExamResult>
            {
                new ExamResult
                {
                    CarePlan = "care plan",
                    GraderFirstName = "first name",
                    GraderLastName = "last name",
                    GraderNpi = "npi",
                    GraderTaxonomy = "taxonomy",
                    ExamDiagnoses = new List<ExamDiagnosis>()
                }
            },
            ExamStatuses = new List<ExamStatus>
            {
                new ExamStatus
                {
                    ExamStatusCodeId = ExamStatusCode.ResultDataDownloaded.ExamStatusCodeId,
                    ReceivedDateTime = resultDownloadedDateTime
                }
            }
        };

        // Act
        var subject = CreateSubject();

        var actual = subject.Map<Result>(source);

        Assert.Equal(source.CreatedDateTime, actual.PerformedDate);
    }

    #region Diagnosis Codes
    [Theory]
    [MemberData(nameof(Convert_DiagnosisCodes_TestData))]
    public void Convert_DiagnosisCodes_Tests(IEnumerable<ExamDiagnosis> diagnoses,
        ICollection<string> expectedDiagnosisCodes)
    {
        // Arrange
        var source = BuildEmptySource();

        foreach (var diagnosis in diagnoses)
        {
            source.ExamResults.First().ExamDiagnoses.Add(diagnosis);
        }

        // Act
        var subject = CreateSubject();

        var actual = subject.Map<Result>(source);

        // Assert
        EnumerableComparer.AssertComparable(expectedDiagnosisCodes, actual.DiagnosisCodes, StringComparer.InvariantCulture);
    }

    public static IEnumerable<object[]> Convert_DiagnosisCodes_TestData()
    {
        yield return new object[]
        {
            new List<ExamDiagnosis>(),
            new List<string>()
        };

        yield return new object[]
        {
            new List<ExamDiagnosis>
            {
                new ExamDiagnosis { Diagnosis = "a" }
            },
            new List<string> { "a" }
        };

        yield return new object[]
        {
            new List<ExamDiagnosis>
            {
                new ExamDiagnosis { Diagnosis = "a" },
                new ExamDiagnosis { Diagnosis = "b" },
                new ExamDiagnosis { Diagnosis = "c" }
            },
            new List<string> { "a", "c", "b" } // Order doesn't matter
        };

        yield return new object[]
        {
            new List<ExamDiagnosis>
            {
                new ExamDiagnosis { Diagnosis = "a" },
                new ExamDiagnosis { Diagnosis = "b" },
                new ExamDiagnosis { Diagnosis = "a" } // Should exclude duplicates
            },
            new List<string> { "a", "b" }
        };

        #region Edge cases
        yield return new object[]
        {
            new List<ExamDiagnosis>
            {
                new ExamDiagnosis { Diagnosis = "a" },
                new ExamDiagnosis { Diagnosis = null },
                new ExamDiagnosis { Diagnosis = "" },
                new ExamDiagnosis { Diagnosis = "  " }
            },
            new List<string> { "a" }
        };
        #endregion Edge cases
    }
    #endregion Diagnosis Codes

    

    // See https://wiki.signifyhealth.com/display/AncillarySvcs/DEE+Business+Rules
    //
    // An exam is billable if *any* of the following are true:
    // 1) There are findings for both left and right eye
    // 2) There are findings for the left eye, and at least one image for the right eye exists
    // 3) There are findings for the right eye, and at least one image for the left eye exists
    [Theory]
    [InlineData(0, 0, 0, 0, false)] // No findings nor images
    [InlineData(1, 0, 0, 0, false)] // One side findings, no images
    [InlineData(0, 1, 0, 0, false)] // One side findings, no images
    [InlineData(2, 0, 0, 0, false)] // One side findings, no images
    [InlineData(0, 2, 0, 0, false)] // One side findings, no images
    [InlineData(0, 0, 1, 0, false)] // No findings, one side images
    [InlineData(0, 0, 0, 1, false)] // No findings, one side images
    [InlineData(0, 0, 1, 1, false)] // No findings, both sides images
    [InlineData(1, 1, 0, 0, true)] // Both sides findings, no images
    [InlineData(1, 1, 1, 0, true)] // Both sides findings, one side images
    [InlineData(1, 1, 0, 1, true)] // Both sides findings, one side images
    [InlineData(1, 1, 1, 1, true)] // Both sides findings, both sides images
    [InlineData(1, 0, 1, 0, false)] // One side findings and images
    [InlineData(0, 2, 0, 2, false)] // One side findings and images
    [InlineData(1, 0, 0, 1, true)] // One side findings, other side images
    [InlineData(0, 1, 1, 0, true)] // One side findings, other side images
    public void Convert_IsBillable_Tests(
        int countLeftFindings, int countRightFindings,
        int countLeftImages, int countRightImages,
        bool expectedIsBillable)
    {
        // Arrange
        var source = BuildEmptySource();

        source.ExamImages = new List<ExamImage>(
            Enumerable.Repeat(new ExamImage { LateralityCodeId = Left }, countLeftImages)
                .Union(Enumerable.Repeat(new ExamImage { LateralityCodeId = Right }, countRightImages))
        );

        source.ExamResults.First().ExamFindings = new List<ExamFinding>(
            Enumerable.Repeat(new ExamFinding { LateralityCodeId = Left, Finding = "a-a" }, countLeftFindings)
                .Union(Enumerable.Repeat(new ExamFinding { LateralityCodeId = Right, Finding = "a-a" }, countRightFindings))
        );

        // Act
        var subject = CreateSubject();

        var actual = subject.Map<Result>(source);

        // Assert
        Assert.Equal(expectedIsBillable, actual.IsBillable);
    }
}