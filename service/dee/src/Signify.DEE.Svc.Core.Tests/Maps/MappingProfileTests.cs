using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using Iris.Public.Types.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Signify.DEE.Messages;
using Signify.DEE.Messages.Status;
using Signify.DEE.Svc.Core.ApiClient.Requests;
using Signify.DEE.Svc.Core.Commands;
using Signify.DEE.Svc.Core.Configs;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Events;
using Signify.DEE.Svc.Core.Events.Status;
using Signify.DEE.Svc.Core.Maps;
using Signify.DEE.Svc.Core.Messages.Commands;
using Signify.DEE.Svc.Core.Messages.Models;
using Signify.DEE.Svc.Core.Messages.Queries;
using Signify.DEE.Svc.Core.Tests.Mocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using DeeNotPerformed = Signify.DEE.Svc.Core.Data.Entities.DeeNotPerformed;

namespace Signify.DEE.Svc.Core.Tests.Maps;

public sealed class MappingProfileTests : IDisposable, IAsyncDisposable
{
    private readonly IMapper _mapper;
    private readonly ServiceProvider _serviceProvider;
    private readonly FakeApplicationTime _applicationTime = new();

    public MappingProfileTests()
    {
        _serviceProvider = new ServiceCollection()
            .AddSingleton(new IrisConfig { ClientGuid = "clientGuid", SiteLocalId = "55" })
            .BuildServiceProvider();

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(
                new MappingProfile());
            cfg.ConstructServicesUsing(ResolveService);
        });

        _mapper = config.CreateMapper();
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _serviceProvider.DisposeAsync();
    }
    private object ResolveService(Type type)
    {
        if (type == typeof(ResultMapper))
            return new ResultMapper(new OverallNormalityMapper(A.Dummy<ILogger<OverallNormalityMapper>>()), _applicationTime);

        return type == typeof(SideResultInfoMapper) ? new SideResultInfoMapper(new OverallNormalityMapper(A.Dummy<ILogger<OverallNormalityMapper>>())) : null;
    }

    [Fact]
    public void Should_Map_ExamModel_To_Exam()
    {
        var source = new Exam { ExamId = 1 };
        var target = _mapper.Map<Exam, ExamModel>(source);
        target.ExamId.Should().Be(1);
    }

    [Fact]
    public void Should_Map_NotPerformedReason_To_DeeNotPerformed()
    {
        //Arrange
        var source = new NotPerformedReason { AnswerId = 123, NotPerformedReasonId = 234, Reason = "Patient Unwilling" };

        //Act
        var target = _mapper.Map<NotPerformedReason, DeeNotPerformed>(source);

        //Assert
        target.NotPerformedReasonId.Should().Be(234);
    }

    [Fact]
    public void Should_Map_ExamImage_To_ExamImageModel()
    {
        var source = new ExamImage { ExamId = 1, LateralityCodeId = 1 };
        var target = _mapper.Map<ExamImage, ExamImageModel>(source);
        target.ExamId.Should().Be(1);
        target.Laterality.Should().Be("OD");
    }

    [Fact]
    public void Should_Map_ExamImage_To_ExamImageModel_WhenLocalIdPresentButNoLaterality()
    {
        var source = new ExamImage { ExamId = 1, ImageLocalId = "565" };
        var target = _mapper.Map<ExamImage, ExamImageModel>(source);
        target.ExamId.Should().Be(1);
        target.Laterality.Should().Be("");
        target.ImageLocalId.Should().Be("565");
    }

    [Fact]
    public void Should_Map_ExamResult_To_ExamResultModel()
    {
        var source = new ExamResult { ExamId = 1 };
        var target = _mapper.Map<ExamResult, ExamResultModel>(source);
        target.ExamId.Should().Be(1);
    }

    #region To ExamImage and ExamImageModel tests

    [Theory]
    [MemberData(nameof(ExamImageModel_ExamImage_MappingTestData))]
    public void ExamImageModel_To_ExamImage_ShouldMap_NotGradableReasons(IEnumerable<string> sourceReasons,
        string expectedNotGradableReasons)
    {
        var model = new ExamImageModel();

        model.NotGradableReasons.AddRange(sourceReasons);

        var examImage = _mapper.Map<ExamImage>(model);

        Assert.Equal(expectedNotGradableReasons, examImage.NotGradableReasons);
    }

    [Fact]
    public void ExamImageModel_To_ExamImage_ShouldMap_Unspecified_Laterality_To_Unknown()
    {
        var model = new ExamImageModel();

        var examImage = _mapper.Map<ExamImage>(model);

        Assert.Equal(examImage.LateralityCode, LateralityCode.Unknown);
    }

    [Theory]
    [MemberData(nameof(ExamImageModel_ExamImage_MappingTestData))]
    public void ExamImage_To_ExamImageModel_ShouldMap_NotGradableReasons(IList<string> expectedNotGradableReasons,
        string sourceReason)
    {
        var examImage = new ExamImage
        {
            NotGradableReasons = sourceReason,
            LateralityCodeId = 1
        };

        var destination = _mapper.Map<ExamImageModel>(examImage);

        Assert.Equal(expectedNotGradableReasons.Count, destination.NotGradableReasons.Count);
        for (int i = 0; i < expectedNotGradableReasons.Count; ++i)
        {
            Assert.Equal(expectedNotGradableReasons[i], destination.NotGradableReasons[i]);
        }
    }


    public static IEnumerable<object[]> ExamImageModel_ExamImage_MappingTestData()
    {
        yield return new object[]
        {
            new List<string>
            {
                "reason 1"
            },
            "reason 1"
        };

        yield return new object[]
        {
            new List<string>
            {
                "reason 1",
                "reason 2"
            },
            "reason 1; reason 2"
        };

        yield return new object[]
        {
            new List<string>
            {
                "reason 1",
                "reason 2",
                "reason 3"
            },
            "reason 1; reason 2; reason 3"
        };

        yield return new object[]
        {
            new List<string>(),
            string.Empty
        };
    }

    #endregion To ExamImage and ExamImageModel tests

    [Fact]
    public void Exam_To_Result_ShouldBeMapped()
    {
        var source = ResultMapperTests.BuildEmptySource(); // Set minimally-required properties

        var actual = _mapper.Map<Result>(source);

        Assert.NotNull(actual);
    }

    [Fact]
    public void Exam_To_ICollectionOfSideResultInfo_ShouldBeMapped()
    {
        var source = ResultMapperTests.BuildEmptySource(); // Set minimally-required properties

        var actual = _mapper.Map<ICollection<SideResultInfo>>(source);

        Assert.NotNull(actual);
    }

    [Fact]
    public void NotPerformedReason_To_NotPerformedModel_ShouldBeMapped()
    {
        var source = new NotPerformedReason { NotPerformedReasonId = 1, Reason = "Test" };
        var target = _mapper.Map<NotPerformedReason, NotPerformedModel>(source);
        target.NotPerformedReasonId.Should().Be(1);
        target.Reason.Should().Be("Test");
    }

    [Fact]
    public void NotPerformedModel_To_DeeNotPerformed_ShouldBeMapped()
    {
        var source = new NotPerformedModel { NotPerformedReasonId = 1, Reason = "Test", ReasonNotes = "Test Notes" };
        var target = _mapper.Map<NotPerformedModel, DeeNotPerformed>(source);
        target.NotPerformedReasonId.Should().Be(1);
        target.Notes.Should().Be("Test Notes");
    }

    [Fact]
    public void NotPerformedModel_To_NotPerformed_ShouldBeMapped()
    {
        var source = new NotPerformedModel
        {
            Reason = "reason",
            ReasonType = "reason type",
            ReasonNotes = "reason notes"
        };
        var actual = _mapper.Map<NotPerformed>(source);
        actual.Reason.Should().Be("reason");
        actual.ReasonType.Should().Be("reason type");
        actual.ReasonNotes.Should().Be("reason notes");
    }

    [Fact]
    public void ExamModel_To_RcmBilling_ShouldBeMapped()
    {
        var source = new ExamModel { ExamId = 1, MemberPlanId = 65743, DateOfService = _applicationTime.UtcNow(), State = "TX", EvaluationObjective = new EvaluationObjective() { EvaluationObjectiveId = 1, Objective = "Comprehensive" } };
        var actual = _mapper.Map<RCMBilling>(source);
        actual.MemberPlanId.Should().Be(65743);
        actual.UsStateOfService.Should().Be("TX");
        actual.DateOfService.Should().Be(_applicationTime.UtcNow());
        actual.RcmProductCode.Should().Be("DEE");
    }

    [Fact]
    public void ExamModel_To_NotPerformed_ShouldBeMapped()
    {
        var source = new ExamModel { ExamId = 1, MemberPlanId = 65743, CreatedDateTime = _applicationTime.UtcNow(), State = "TX", RetinalImageTestingNotes = "Test notes" };
        var actual = _mapper.Map<NotPerformed>(source);
        actual.MemberPlanId.Should().Be(65743);
        actual.CreateDate.Should().Be(_applicationTime.UtcNow());
        actual.RetinalImageTestingNotes.Should().Be("Test notes");
    }

    [Fact]
    public void ExamModel_To_Performed_ShouldBeMapped()
    {
        var source = new ExamModel { ExamId = 1, MemberPlanId = 65743, CreatedDateTime = _applicationTime.UtcNow(), State = "TX", RetinalImageTestingNotes = "Test notes" };
        var actual = _mapper.Map<Performed>(source);
        actual.MemberPlanId.Should().Be(65743);
        actual.CreateDate.Should().Be(_applicationTime.UtcNow());
        actual.RetinalImageTestingNotes.Should().Be("Test notes");
    }



    [Fact]
    public void MapProcessIrisOrderResult_MapperReturnsCreateExamResultRecord_GradableImageTrue()
    {
        // Arrange
        var examModel = new ExamModel
        { DateOfService = _applicationTime.UtcNow(), MemberPlanId = 97, EvaluationId = 75, ExamId = 46 };

        var source = new ProcessIrisOrderResult
        {
            Exam = examModel,
            OrderResult = OrderResultMock.BuildOrderResult()
        };

        // Act
        var target = _mapper.Map<ProcessIrisOrderResult, ExamResultModel>(source);

        // Assert
        target.ExamResultId.Should().Be(100123);
        target.ExamId.Should().Be(46);
        target.CarePlan.Should().Be("Return in 6 months");
        target.DateSigned.Should().NotBeNull();
        target.Diagnoses.Count.Should().Be(1);
        target.Diagnoses.FirstOrDefault().Should().Be("E083211");
        target.LeftEyeHasPathology.Should().BeTrue();
        target.RightEyeHasPathology.Should().BeTrue();
        target.LeftEyeFindings.Count.Should().Be(1);
        target.RightEyeFindings.Count.Should().Be(1);
        target.RightEyeFindings.FirstOrDefault().Should().Be("Diabetic Retinopathy - Mild");
        target.GradableImage.Should().BeTrue();
        target.Grader.Should().NotBeNull();
        target.Grader.FirstName.Should().Be("John");
        target.Grader.LastName.Should().Be("Doe");
        target.Grader.NPI.Should().Be("1234567890");
        target.Grader.Taxonomy.Should().Be("207W00000X");
    }

    [Fact]
    public void MapProcessIrisOrderResult_MapperReturnsCreateExamResultRecord_GradableImageFalse()
    {
        // Arrange
        var examModel = new ExamModel
        { DateOfService = _applicationTime.UtcNow(), MemberPlanId = 67, EvaluationId = 45, ExamId = 23 };

        var source = new ProcessIrisOrderResult
        {
            Exam = examModel,
            OrderResult = OrderResultMock.BuildOrderResult()
        };
        source.OrderResult.ImageDetails.TotalCount = 1;
        source.OrderResult.ImageDetails.RightEyeCount = 0;

        // Act
        var target = _mapper.Map<ProcessIrisOrderResult, ExamResultModel>(source);

        // Assert
        target.ExamResultId.Should().Be(100123);
        target.ExamId.Should().Be(23);
        target.CarePlan.Should().Be("Return in 6 months");
        target.DateSigned.Should().NotBeNull();
        target.Diagnoses.Count.Should().Be(1);
        target.Diagnoses.FirstOrDefault().Should().Be("E083211");
        target.LeftEyeHasPathology.Should().BeTrue();
        target.RightEyeHasPathology.Should().BeTrue();
        target.LeftEyeFindings.Count.Should().Be(1);
        target.RightEyeFindings.Count.Should().Be(1);
        target.RightEyeFindings.FirstOrDefault().Should().Be("Diabetic Retinopathy - Mild");
        target.GradableImage.Should().BeFalse();
        target.Grader.Should().NotBeNull();
        target.Grader.FirstName.Should().Be("John");
        target.Grader.LastName.Should().Be("Doe");
        target.Grader.NPI.Should().Be("1234567890");
        target.Grader.Taxonomy.Should().Be("207W00000X");
        target.LeftEyeGradable.Should().BeTrue();
        target.RightEyeGradable.Should().BeTrue();
    }

    [Fact]
    public void MapProcessIrisOrderResult_MapperReturnsCreateExamResultRecord_GradableImageTrueForSingleEye()
    {
        // Arrange
        var examModel = new ExamModel
        { DateOfService = _applicationTime.UtcNow(), MemberPlanId = 45, EvaluationId = 56, ExamId = 67 };

        var source = new ProcessIrisOrderResult
        {
            Exam = examModel,
            OrderResult = OrderResultMock.BuildOrderResult()
        };
        source.OrderResult.ImageDetails.TotalCount = 1;
        source.OrderResult.ImageDetails.RightEyeCount = 0;
        source.OrderResult.ImageDetails.SingleEyeOnly = true;

        // Act
        var target = _mapper.Map<ProcessIrisOrderResult, ExamResultModel>(source);

        // Assert
        target.ExamResultId.Should().Be(100123);
        target.ExamId.Should().Be(67);
        target.CarePlan.Should().Be("Return in 6 months");
        target.DateSigned.Should().NotBeNull();
        target.Diagnoses.Count.Should().Be(1);
        target.Diagnoses.FirstOrDefault().Should().Be("E083211");
        target.LeftEyeHasPathology.Should().BeTrue();
        target.RightEyeHasPathology.Should().BeTrue();
        target.LeftEyeFindings.Count.Should().Be(1);
        target.RightEyeFindings.Count.Should().Be(1);
        target.RightEyeFindings.FirstOrDefault().Should().Be("Diabetic Retinopathy - Mild");
        target.GradableImage.Should().BeTrue();
        target.Grader.Should().NotBeNull();
        target.Grader.FirstName.Should().Be("John");
        target.Grader.LastName.Should().Be("Doe");
        target.Grader.NPI.Should().Be("1234567890");
        target.Grader.Taxonomy.Should().Be("207W00000X");
    }
    
    [Fact]
    public void MapProcessIrisOrderResult_MapperReturnsCreateExamResultRecord_GradingDateMapsToIrisGradingDate()
    {
        // Arrange
        var examModel = new ExamModel
            { DateOfService = _applicationTime.UtcNow(), MemberPlanId = 56, EvaluationId = 98, ExamId = 32 };

        var source = new ProcessIrisOrderResult
        {
            Exam = examModel,
            OrderResult = OrderResultMock.BuildOrderResult()
        };
        var examGradedTime = source.OrderResult!.Gradings!.GradedTime;

        // Act
        var target = _mapper.Map<ProcessIrisOrderResult, ExamResultModel>(source);

        // Assert
        target.DateSigned.Should().NotBeNull();
        target.DateSigned!.Value.Date.Should().Be(examGradedTime.Date);
    }

    [Fact]
    public void MapProcessIrisOrderResult_BothEyeGradingsHavePathology_PathologyTrueForBothImages()
    {
        // Arrange
        var examModel = new ExamModel
        { DateOfService = _applicationTime.UtcNow(), MemberPlanId = 68, EvaluationId = 34, ExamId = 67 };

        var odFindings = new List<ResultFinding>
        {
            new()
            {
                Finding = "Diabetic Retinopathy",
                Result = "Mild"
            },
            new()
            {
                Finding = "Macular Edema",
                Result = "None"
            }
        };

        var osFindings = new List<ResultFinding>
        {
            new()
            {
                Finding = "Diabetic Retinopathy",
                Result = "None"
            },
            new()
            {
                Finding = "Macular Edema",
                Result = "Positive"
            }
        };

        var source = new ProcessIrisOrderResult
        {
            Exam = examModel,
            OrderResult = OrderResultMock.BuildOrderResultWithCustomResults(odFindings, osFindings)
        };

        // Act
        var target = _mapper.Map<ProcessIrisOrderResult, ExamResultModel>(source);

        // Assert
        target.LeftEyeHasPathology.Should().BeTrue();
        target.RightEyeHasPathology.Should().BeTrue();
    }

    [Fact]
    public void MapProcessIrisOrderResult_OnlyLeftEyeGradingsHavePathology_PathologyTrueForJustLeftEye()
    {
        // Arrange
        var examModel = new ExamModel
        { DateOfService = _applicationTime.UtcNow(), MemberPlanId = 65, EvaluationId = 46, ExamId = 56 };

        var odFindings = new List<ResultFinding>
        {
            new()
            {
                Finding = "Diabetic Retinopathy",
                Result = "None"
            },
            new()
            {
                Finding = "Macular Edema",
                Result = "None"
            }
        };

        var osFindings = new List<ResultFinding>
        {
            new()
            {
                Finding = "Diabetic Retinopathy",
                Result = "Severe"
            },
            new()
            {
                Finding = "Macular Edema",
                Result = "Positive"
            }
        };

        var source = new ProcessIrisOrderResult
        {
            Exam = examModel,
            OrderResult = OrderResultMock.BuildOrderResultWithCustomResults(odFindings, osFindings)
        };

        // Act
        var target = _mapper.Map<ProcessIrisOrderResult, ExamResultModel>(source);

        // Assert
        target.LeftEyeHasPathology.Should().BeTrue();
        target.RightEyeHasPathology.Should().BeFalse();
    }

    [Fact]
    public void MapProcessIrisOrderResult_OnlyRightEyeGradingsHavePathology_PathologyTrueForJustRightEye()
    {
        // Arrange
        var examModel = new ExamModel
        { DateOfService = _applicationTime.UtcNow(), MemberPlanId = 5, EvaluationId = 34545, ExamId = 23423 };

        var odFindings = new List<ResultFinding>
        {
            new()
            {
                Finding = "Diabetic Retinopathy",
                Result = "Severe"
            },
            new()
            {
                Finding = "Macular Edema",
                Result = "Positive"
            }
        };

        var osFindings = new List<ResultFinding>
        {
            new()
            {
                Finding = "Diabetic Retinopathy",
                Result = "None"
            },
            new()
            {
                Finding = "Macular Edema",
                Result = "None"
            }
        };

        var source = new ProcessIrisOrderResult
        {
            Exam = examModel,
            OrderResult = OrderResultMock.BuildOrderResultWithCustomResults(odFindings, osFindings)
        };

        // Act
        var target = _mapper.Map<ProcessIrisOrderResult, ExamResultModel>(source);

        // Assert
        target.LeftEyeHasPathology.Should().BeFalse();
        target.RightEyeHasPathology.Should().BeTrue();
    }

    [Fact]
    public void MapProcessIrisOrderResult_Both_Eyes_Have_Third_Finding_PathologyTrueForBothEyes()
    {
        // Arrange
        var examModel = new ExamModel
        { DateOfService = _applicationTime.UtcNow(), MemberPlanId = 45, EvaluationId = 45687, ExamId = 56743 };

        var odFindings = new List<ResultFinding>
        {
            new()
            {
                Finding = "Diabetic Retinopathy",
                Result = "None"
            },
            new()
            {
                Finding = "Macular Edema",
                Result = "None"
            },
            new()
            {
                Finding = "Other",
                Result = "Please contact PCP"
            }
        };

        var osFindings = new List<ResultFinding>
        {
            new()
            {
                Finding = "Diabetic Retinopathy",
                Result = "None"
            },
            new()
            {
                Finding = "Macular Edema",
                Result = "None"
            },
            new()
            {
                Finding = "Wet AMD and HTN",
                Result = ""
            }
        };

        var source = new ProcessIrisOrderResult
        {
            Exam = examModel,
            OrderResult = OrderResultMock.BuildOrderResultWithCustomResults(odFindings, osFindings)
        };

        // Act
        var target = _mapper.Map<ProcessIrisOrderResult, ExamResultModel>(source);

        // Assert
        target.LeftEyeHasPathology.Should().BeTrue();
        target.RightEyeHasPathology.Should().BeTrue();
        target.LeftEyeFindings.Any(x => x.StartsWith("Other")).Should().BeTrue();
        target.RightEyeFindings.Any(x => x.StartsWith("Other")).Should().BeTrue();
    }

    [Fact]
    public void ProviderPayStatusEvent_To_ProviderPayableEventReceived()
    {
        var source = new ProviderPayStatusEvent
        {
            ExamId = 1,
            EvaluationId = 12,
            MemberPlanId = 65743,
            ProviderId = 10,
            ParentCdiEvent = "CDIPassedEvent",
            ParentEventReceivedDateTime = _applicationTime.UtcNow().AddMinutes(3),
            Reason = "Test reason"
        };
        var actual = _mapper.Map<ProviderPayableEventReceived>(source);
        actual.MemberPlanId.Should().Be(source.MemberPlanId);
        actual.ProviderId.Should().Be((int)source.ProviderId);
        actual.EvaluationId.Should().Be(source.EvaluationId);
        actual.CreateDate.Should().Be(default);
        actual.ProductCode.Should().Be("DEE");
        actual.ReceivedDate.Should().Be(source.ParentEventReceivedDateTime);
        actual.ParentCdiEvent.Should().Be(source.ParentCdiEvent);
    }

    [Fact]
    public void ProviderPayStatusEvent_To_ProviderNonPayableEventReceived()
    {
        var source = new ProviderPayStatusEvent
        {
            ExamId = 1,
            EvaluationId = 12,
            MemberPlanId = 65743,
            ProviderId = 10,
            ParentCdiEvent = "CDIPassedEvent",
            ParentEventReceivedDateTime = _applicationTime.UtcNow().AddMinutes(3),
            Reason = "Test reason"
        };
        var actual = _mapper.Map<ProviderNonPayableEventReceived>(source);
        actual.MemberPlanId.Should().Be(source.MemberPlanId);
        actual.ProviderId.Should().Be((int)source.ProviderId);
        actual.EvaluationId.Should().Be(source.EvaluationId);
        actual.CreateDate.Should().Be(default);
        actual.ProductCode.Should().Be("DEE");
        actual.ReceivedDate.Should().Be(source.ParentEventReceivedDateTime);
        actual.ParentCdiEvent.Should().Be(source.ParentCdiEvent);
        actual.Reason.Should().Be(source.Reason);
    }


    [Fact]
    public void ProviderPayRequest_To_ProviderPayApiRequest()
    {
        var dos = _applicationTime.UtcNow().ToString("o");
        var source = new ProviderPayRequest
        {
            ProviderProductCode = "DEE",
            ProviderId = 10,
            PersonId = "XName",
            DateOfService = dos,
            ClientId = 30,
            AdditionalDetails = new Dictionary<string, string>
            {
                { "name", "value" }
            }
        };
        var actual = _mapper.Map<ProviderPayApiRequest>(source);
        actual.ProviderProductCode.Should().Be("DEE");
        actual.ProviderId.Should().Be(10);
        actual.PersonId.Should().Be("XName");
        actual.ClientId.Should().Be(30);
        actual.DateOfService.Should().Be(dos);
        actual.AdditionalDetails.Should().NotBeEmpty();
    }

    [Fact]
    public void MemberModel_To_MemberInfoRs()
    {
        var source = new MemberModel
        {
            FirstName = "First",
            MiddleName = "Middle",
            LastName = "Last",
            DateOfBirth = DateTime.Today,
            AddressLineOne = "123",
            AddressLineTwo = "Address Lane",
            City = "City",
            State = "State",
            ZipCode = "V123456",
            Client = "Client",
            CenseoId = "X12345"
        };
        var actual = _mapper.Map<MemberInfoRs>(source);
        actual.FirstName.Should().Be("First");
        actual.MiddleName.Should().Be("Middle");
        actual.LastName.Should().Be("Last");
        actual.DateOfBirth.Should().Be(DateTime.Today);
        actual.AddressLineOne.Should().Be("123");
        actual.AddressLineTwo.Should().Be("Address Lane");
        actual.City.Should().Be("City");
        actual.State.Should().Be("State");
        actual.ZipCode.Should().Be("V123456");
        actual.Client.Should().Be("Client");
        actual.CenseoId.Should().Be("X12345");
    }

    [Fact]
    public void Exam_To_ProviderPayRequest()
    {
        var dos = new DateTime(2023, 1, 21);
        const string dateString = "2023-01-21";
        var source = new Exam
        {
            ExamId = 1,
            EvaluationId = 12,
            MemberPlanId = 65743,
            ProviderId = 10,
            CreatedDateTime = DateTimeOffset.UtcNow.DateTime,
            DateOfService = dos,
            ClientId = 15,
        };
        var actual = _mapper.Map<ProviderPayRequest>(source);
        actual.MemberPlanId.Should().Be(65743);
        actual.ProviderId.Should().Be(10);
        actual.EvaluationId.Should().Be(12);
        actual.ExamId.Should().Be(1);
        actual.DateOfService.Should().Be(dateString);
    }

    [Fact]
    public void ExamModel_To_ProviderPayRequest()
    {
        var dos = new DateTime(2023, 1, 21);
        var dateString = "2023-01-21";
        var source = new ExamModel
        {
            ExamId = 1,
            EvaluationId = 12,
            MemberPlanId = 65743,
            ProviderId = 10,
            CreatedDateTime = DateTimeOffset.UtcNow.DateTime,
            DateOfService = dos,
            ClientId = 15,
        };
        var actual = _mapper.Map<ProviderPayRequest>(source);
        actual.MemberPlanId.Should().Be(65743);
        actual.ProviderId.Should().Be(10);
        actual.EvaluationId.Should().Be(12);
        actual.ExamId.Should().Be(1);
        actual.DateOfService.Should().Be(dateString);
    }

    [Fact]
    public void Exam_To_ProviderPayStatusEvent()
    {
        var exam = new Exam
        {
            EvaluationObjective = new EvaluationObjective { EvaluationObjectiveId = 1, Objective = "Focused" }
        };

        var actual = _mapper.Map<ProviderPayStatusEvent>(exam);

        actual.ProductCode.Should().Be("DEE-DFV");
    }

    [Theory]
    [InlineData("CDIPassedEvent", 26)]
    [InlineData("CDIFailedEvent", 27)]
    public void ExamStatus_To_ProviderPayRequest(string parentCdiEvent, int statusCodeId)
    {
        var source = new ExamStatus
        {
            ExamStatusId = 1,
            ExamId = 2,
            ReceivedDateTime = _applicationTime.UtcNow().AddMinutes(-5),
            ExamStatusCodeId = statusCodeId,
            CreatedDateTime = _applicationTime.UtcNow(),
            DeeEventId = Guid.NewGuid(),
        };
        var actual = _mapper.Map<ProviderPayRequest>(source);
        actual.MemberPlanId.Should().Be(default);
        actual.ProviderId.Should().Be(default);
        actual.EvaluationId.Should().Be(default);
        actual.ExamId.Should().Be(default);
        actual.DateOfService.Should().Be(default);
        actual.PersonId.Should().Be(default);
        actual.ClientId.Should().Be(default);
        actual.ProviderProductCode.Should().Be(default);
        actual.ParentEventDateTime.Should().Be(source.CreatedDateTime);
        actual.ParentEventReceivedDateTime.Should().Be(source.ReceivedDateTime);
        actual.EventId.Should().Be(Guid.Empty);
        Assert.Null(actual.AdditionalDetails);
        actual.ParentEvent.Should().Be(parentCdiEvent);
    }

    [Theory]
    [InlineData("DEE")]
    [InlineData("DEE-DFV")]
    public void ProviderPayStatusEvent_To_ProviderPayRequestSent(string productCode)
    {
        var source = new ProviderPayStatusEvent
        {
            ExamId = 1,
            EvaluationId = 12,
            MemberPlanId = 65743,
            ProviderId = 10,
            PaymentId = Guid.NewGuid().ToString(),
            StatusDateTime = _applicationTime.UtcNow(),
            ParentEventReceivedDateTime = _applicationTime.UtcNow(),
            EventId = Guid.NewGuid(),
            ParentCdiEvent = "CDIPassedEvent",
            ProductCode = productCode
        };

        var actual = _mapper.Map<ProviderPayRequestSent>(source);

        actual.MemberPlanId.Should().Be(source.MemberPlanId);
        actual.ProviderId.Should().Be((int)source.ProviderId);
        actual.ProductCode.Should().Be("DEE");
        actual.EvaluationId.Should().Be(source.EvaluationId);
        actual.ReceivedDate.Should().Be(source.ParentEventReceivedDateTime);
        actual.PaymentId.Should().Be(source.PaymentId);
        actual.ParentEventDateTime.Should().Be(source.StatusDateTime);
        actual.CreateDate.Should().Be(default);
        actual.ProviderPayProductCode.Should().Be(productCode);
    }

    [Fact]
    public void CdiEventBaseAndExam_To_ProviderPayStatusEvent()
    {
        var cdiSource = new CDIPassedEvent
        {
            DateTime = _applicationTime.UtcNow(),
            ReceivedByDeeDateTime = _applicationTime.UtcNow().AddMinutes(2),
            EvaluationId = 123,
            RequestId = Guid.NewGuid(),
            Products = new List<DpsProduct>(),
            ApplicationId = "ABC",
            UserName = "DEF"
        };

        var actual = _mapper.Map<ProviderPayStatusEvent>(cdiSource);

        actual.MemberPlanId.Should().Be(0);
        actual.ProviderId.Should().Be(0);
        actual.EvaluationId.Should().Be(cdiSource.EvaluationId);
        actual.ParentEventReceivedDateTime.Should().Be(cdiSource.ReceivedByDeeDateTime);
        actual.StatusDateTime.Should().Be(cdiSource.DateTime);
        actual.EventId.Should().Be(cdiSource.RequestId);
        actual.ParentCdiEvent.Should().Be("CDIPassedEvent");

        var dos = new DateTime(2023, 1, 21);
        var examSource = new Exam
        {
            ExamId = 1,
            EvaluationId = 12,
            MemberPlanId = 65743,
            ProviderId = 10,
            CreatedDateTime = DateTimeOffset.UtcNow.DateTime,
            DateOfService = dos,
            ClientId = 15,
        };

        _mapper.Map(examSource, actual);
        actual.MemberPlanId.Should().Be(examSource.MemberPlanId);
        actual.ProviderId.Should().Be(examSource.ProviderId);
        actual.EvaluationId.Should().Be(examSource.EvaluationId);
        actual.ExamId.Should().Be(examSource.ExamId);
    }
}