using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using Iris.Public.Types.Enums;
using Iris.Public.Types.Models;
using Signify.DEE.Svc.Core.Constants;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Messages.Commands;
using Signify.DEE.Svc.Core.Messages.Models;
using Signify.DEE.Svc.Core.Tests.Mocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Messages.Commands;

public class CreateExamImageModelRecordsHandlerTests
{
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly CreateExamImageModelRecordsHandler _handler;

    public CreateExamImageModelRecordsHandlerTests()
    {
        _handler = new CreateExamImageModelRecordsHandler(_mapper);
    }

    [Fact]
    public async Task CreateExamImageModelRecordsHandler_ImagesPassedIn_MappedCorrectlyToModel()
    {
        // Arrange
        var examImages = new List<ExamImage>
        {
            ExamImageEntityMock.BuildExamImage(100, 1),
            ExamImageEntityMock.BuildExamImage(200, 2)
        };

        var irisImages = new List<ResultImage>();

        var request = new CreateExamImageModelRecords
        {
            DbImages = examImages,
            IrisImages = irisImages,
            Gradings = ResultGradingMock.BuildResultGrading(true, new List<string>(), true, new List<string>())
        };

        A.CallTo(() => _mapper.Map<ExamImageModel>(examImages.FirstOrDefault(x => x.LateralityCodeId == 1))).Returns(new ExamImageModel
        {
            ExamId = 1,
            Gradable = false,
            ImageId = 100,
            Laterality = ApplicationConstants.Laterality.RightEyeCode
        });
        A.CallTo(() => _mapper.Map<ExamImageModel>(examImages.FirstOrDefault(x => x.LateralityCodeId == 2))).Returns(new ExamImageModel
        {
            ExamId = 1,
            Gradable = false,
            ImageId = 200,
            Laterality = ApplicationConstants.Laterality.LeftEyeCode
        });

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        result.Count.Should().Be(2);
    }

    [Fact]
    public async Task CreateExamImageModelRecordsHandler_ImagesPassedInFromServiceBus_MappedCorrectlyToModel()
    {
        // Arrange
        var examImages = new List<ExamImage>
        {
            ExamImageEntityMock.BuildExamImageFromServiceBus(100, "500"),
            ExamImageEntityMock.BuildExamImageFromServiceBus(200, "600"),
        };

        var irisImages = new List<ResultImage>();

        var request = new CreateExamImageModelRecords
        {
            DbImages = examImages,
            IrisImages = irisImages,
            Gradings = ResultGradingMock.BuildResultGrading(true, new List<string>(), true, new List<string>())
        };

        A.CallTo(() => _mapper.Map<ExamImageModel>(examImages.FirstOrDefault(x => x.ImageLocalId == "500"))).Returns(new ExamImageModel
        {
            ExamId = 1,
            Gradable = false,
            ImageLocalId = "500"
        });
        A.CallTo(() => _mapper.Map<ExamImageModel>(examImages.FirstOrDefault(x => x.ImageLocalId == "600"))).Returns(new ExamImageModel
        {
            ExamId = 1,
            Gradable = false,
            ImageLocalId = "600",
        });

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        result.Count.Should().Be(2);
    }

    [Theory]
    [MemberData(nameof(Handle_UnavailableSideGrading_TestData))]
    public async Task CreateExamImageModelRecordsHandler_GradingInfo_NotAvailable(int examId, int firstImageId, int secondImageId, bool? osGradable, List<string> osNonGradableReason, bool? odGradable, List<string> odNonGradableReason, bool expectedLeftEyeGrade, bool expectedRightEyeGrade)
    {
        // Arrange
        var examImages = new List<ExamImage>
        {
            ExamImageEntityMock.BuildExamImage(firstImageId, 1),
            ExamImageEntityMock.BuildExamImage(secondImageId, 2)
        };

        var irisImages = new List<ResultImage>();

        var request = new CreateExamImageModelRecords
        {
            DbImages = examImages,
            IrisImages = irisImages,
            Gradings = ResultGradingMock.BuildResultGrading(osGradable, osNonGradableReason, odGradable, odNonGradableReason)
        };

        A.CallTo(() => _mapper.Map<ExamImageModel>(examImages.FirstOrDefault(x => x.LateralityCodeId == 1))).Returns(new ExamImageModel
        {
            ExamId = examId,
            Gradable = false,
            ImageId = firstImageId,
            Laterality = ApplicationConstants.Laterality.RightEyeCode
        });
        A.CallTo(() => _mapper.Map<ExamImageModel>(examImages.FirstOrDefault(x => x.LateralityCodeId == 2))).Returns(new ExamImageModel
        {
            ExamId = examId,
            Gradable = false,
            ImageId = secondImageId,
            Laterality = ApplicationConstants.Laterality.LeftEyeCode
        });

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        result.Count.Should().Be(2);
        var odResult = result.FirstOrDefault(x => x.Laterality == ApplicationConstants.Laterality.RightEyeCode);
        var osResult = result.FirstOrDefault(x => x.Laterality == ApplicationConstants.Laterality.LeftEyeCode);
        osResult.Gradable.Equals(expectedLeftEyeGrade);
        odResult.Gradable.Equals(expectedRightEyeGrade);
    }

    public static IEnumerable<object[]> Handle_UnavailableSideGrading_TestData()
    {
        //OS and OD both not available
        yield return new object[]
        {
            1, 600, 601, null, null, null, null,false,false
        };
        //Only OS available and gradable
        yield return new object[]
        {
            2, 700, 701, true, new List<string>(), null, null, true, false
        };
        //Only OD available and gradable
        yield return new object[]
        {
            3, 800, 801, null, null, true, new List<string>(), false, true
        };
    }

    [Fact]
    public async Task Handle_HandlesAllIrisLateralityEnumerations_Test()
    {
        // Arrange
        const string orderImageId = "SomeValue";

        var request = new CreateExamImageModelRecords
        {
            DbImages = new[]
            {
                new ExamImage
                {
                    ImageLocalId = orderImageId
                }
            },
            IrisImages = new[]
            {
                new ResultImage
                {
                    OrderImageID = 1,
                    LocalId = orderImageId
                }
            }
        };

        foreach (var laterality in Enum.GetValues<Laterality>())
        {
            request.IrisImages.First().Laterality = laterality;

            // Act
            // Assert
            await _handler.Handle(request, default); // This would throw a NotImplementedException if Iris adds a new enumeration to their library and we don't handle it in code
        }

        Assert.True(true);
    }

    [Theory] // Will be removed in ANC-3730
#pragma warning disable CS0618 // Type or member is obsolete
    [MemberData(nameof(Handle_SetsDeprecatedExamImageValues_TestData))]
    public async Task Handle_SetsDeprecatedExamImageValues_Tests(Laterality laterality, ResultEyeSideGrading grading, ExamImageModel expected)
    {
        // Arrange
        const string orderImageId = "SomeValue";

        var request = new CreateExamImageModelRecords
        {
            DbImages = new[]
            {
                new ExamImage
                {
                    ImageLocalId = orderImageId
                }
            },
            IrisImages = new[]
            {
                new ResultImage
                {
                    OrderImageID = 1,
                    LocalId = orderImageId,
                    Laterality = laterality
                }
            },
            Gradings = new ResultGrading()
        };

        if (laterality == Laterality.OS)
            request.Gradings.OS = grading;
        else
            request.Gradings.OD = grading;

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        Assert.Single(result);

        var actual = result.First();

        Assert.Equal(expected.Gradable, actual.Gradable);
        Assert.Equal(expected.NotGradableReasons.Count, actual.NotGradableReasons.Count);
        for (var i = 0; i < expected.NotGradableReasons.Count; ++i)
        {
            Assert.Equal(expected.NotGradableReasons[i], actual.NotGradableReasons[i]);
        }
    }

    public static IEnumerable<object[]> Handle_SetsDeprecatedExamImageValues_TestData()
    {
        yield return new object[]
        {
            Laterality.OS,
            null,
            new ExamImageModel()
        };

        yield return new object[]
        {
            Laterality.OS,
            new ResultEyeSideGrading
            {
                Gradable = null
            },
            new ExamImageModel()
        };

        yield return new object[]
        {
            Laterality.OS,
            new ResultEyeSideGrading
            {
                Gradable = true,
                UngradableReasons = new []
                {
                    "some reason" // should be ignored since it's gradable
                }
            },
            new ExamImageModel
            {
                Gradable = true
            }
        };

        yield return new object[]
        {
            Laterality.OS,
            new ResultEyeSideGrading
            {
                Gradable = false,
                UngradableReasons = new []
                {
                    "some reason",
                    "another"
                }
            },
            new ExamImageModel
            {
                Gradable = false,
                NotGradableReasons = new List<string>
                {
                    "some reason",
                    "another"
                }
            }
        };

        yield return new object[]
        {
            Laterality.OD,
            new ResultEyeSideGrading
            {
                Gradable = false,
                UngradableReasons = new []
                {
                    "some reason",
                    "another"
                }
            },
            new ExamImageModel
            {
                Gradable = false,
                NotGradableReasons = new List<string>
                {
                    "some reason",
                    "another"
                }
            }
        };
    }
#pragma warning restore CS0618 // Type or member is obsolete
}