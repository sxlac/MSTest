using AutoMapper;
using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Signify.Spirometry.Core.ApiClients.CdiApi.Flags.Requests;
using Signify.Spirometry.Core.ApiClients.MemberApi.Responses;
using Signify.Spirometry.Core.ApiClients.ProviderPayApi.Requests;
using Signify.Spirometry.Core.ApiClients.RcmApi.Requests;
using Signify.Spirometry.Core.Commands;
using Signify.Spirometry.Core.Configs.Exam;
using Signify.Spirometry.Core.Configs.Loopback;
using Signify.Spirometry.Core.Converters;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Events.Akka;
using Signify.Spirometry.Core.Events.Status;
using Signify.Spirometry.Core.Events;
using Signify.Spirometry.Core.Infrastructure;
using Signify.Spirometry.Core.Maps;
using Signify.Spirometry.Core.Models;
using Signify.Spirometry.Core.Services.Flags;
using Signify.Spirometry.Core.Services;
using Signify.Spirometry.Core.Validators;
using SpiroEvents;
using SpiroNsb.SagaEvents;
using SpiroNsbEvents;
using System.Collections.Generic;
using System;
using Xunit;
using ExamStatusEvent = Signify.Spirometry.Core.Events.ExamStatusEvent;
using NormalityIndicator = Signify.Spirometry.Core.Models.NormalityIndicator;
using NotPerformedReason = Signify.Spirometry.Core.Models.NotPerformedReason;
using OccurrenceFrequency = Signify.Spirometry.Core.Models.OccurrenceFrequency;
using PdfDeliveredToClient = Signify.Spirometry.Core.Data.Entities.PdfDeliveredToClient;
using SessionGrade = Signify.Spirometry.Core.Models.SessionGrade;
using StatusCode = Signify.Spirometry.Core.Models.StatusCode;
using TrileanType = Signify.Spirometry.Core.Models.TrileanType;

namespace Signify.Spirometry.Core.Tests.Maps;

public class MappingProfileTests
{
    private static IMapper CreateMapper()
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton(A.Dummy<IFev1Config>())
            .AddSingleton(A.Dummy<IFvcConfig>())
            .AddSingleton<FvcValidator>()
            .AddSingleton<IFvcValidator>(sp => sp.GetRequiredService<FvcValidator>())
            .AddSingleton<Fev1Validator>()
            .AddSingleton<IFev1Validator>(sp => sp.GetRequiredService<Fev1Validator>())
            .AddSingleton<IFev1FvcRatioValidator>(new Fev1FvcRatioValidator())
            .AddSingleton<OverallNormalityConverter>()
            .AddSingleton<IOverallNormalityConverter>(sp => sp.GetRequiredService<OverallNormalityConverter>())
            .AddSingleton<FvcNormalityConverter>()
            .AddSingleton<IFvcNormalityConverter>(sp => sp.GetRequiredService<FvcNormalityConverter>())
            .AddSingleton<Fev1NormalityConverter>()
            .AddSingleton<IFev1NormalityConverter>(sp => sp.GetRequiredService<Fev1NormalityConverter>())
            .AddSingleton<IApplicationTime>(new FakeApplicationTime())
            .AddSingleton<ExamQualityService>()
            .AddSingleton<IExamQualityService>(sp => sp.GetRequiredService<ExamQualityService>())
            .AddSingleton(new LoopbackConfig
            {
                Diagnoses = new[] { new DiagnosisConfig { Name = "COPD", AnswerValue = "some value" } }
            })
            .AddSingleton(A.Fake<IGetLoopbackConfig>())
            .AddSingleton<FlagTextFormatter>()
            .AddSingleton<IFlagTextFormatter>(sp => sp.GetRequiredService<FlagTextFormatter>());

        return new MapperConfiguration(configure =>
        {
            configure.AddProfile<MappingProfile>();
            configure.ConstructServicesUsing(type =>
                ActivatorUtilities.CreateInstance(services.BuildServiceProvider(), type));
        }).CreateMapper();
    }

    [Fact]
    public void Map_FromEvalReceived_To_SpirometryExam_MapsAllProperties()
    {
        var date = new DateTime(2020, 01, 01, 01, 01, 01, DateTimeKind.Utc);

        const int evaluationId = 1;
        const int providerId = 2;
        const long memberId = 3;
        const int memberPlanId = 4;
        var createdDateTime = new DateTimeOffset(date);
        var receivedDateTime = date.AddMinutes(1);
        var dateOfService = date.Date;
        var receivedBySpiro = receivedDateTime.AddMinutes(1);

        var evalReceived = new EvalReceived
        {
            ApplicationId = nameof(ApplicationId),
            EvaluationId = evaluationId,
            ProviderId = providerId,
            MemberId = memberId,
            MemberPlanId = memberPlanId,
            CreatedDateTime = createdDateTime,
            ReceivedDateTime = receivedDateTime,
            DateOfService = dateOfService,
            ReceivedBySpirometryProcessManagerDateTime = receivedBySpiro
        };

        var expectedExam = new SpirometryExam
        {
            ApplicationId = nameof(ApplicationId),
            EvaluationId = evaluationId,
            ProviderId = providerId,
            MemberId = memberId,
            MemberPlanId = memberPlanId,
            EvaluationCreatedDateTime = createdDateTime.DateTime,
            EvaluationReceivedDateTime = receivedDateTime,
            DateOfService = dateOfService,
            CreatedDateTime = receivedBySpiro
        };

        var mapper = CreateMapper();

        var actualExam = mapper.Map<SpirometryExam>(evalReceived);

        Assert.Equal(expectedExam.ApplicationId, actualExam.ApplicationId);
        Assert.Equal(expectedExam.EvaluationId, actualExam.EvaluationId);
        Assert.Equal(expectedExam.ProviderId, actualExam.ProviderId);
        Assert.Equal(expectedExam.MemberId, actualExam.MemberId);
        Assert.Equal(expectedExam.MemberPlanId, actualExam.MemberPlanId);
        Assert.Equal(expectedExam.EvaluationCreatedDateTime, actualExam.EvaluationCreatedDateTime);
        Assert.Equal(expectedExam.EvaluationReceivedDateTime, actualExam.EvaluationReceivedDateTime);
        Assert.Equal(expectedExam.DateOfService, actualExam.DateOfService);
        Assert.Equal(expectedExam.CreatedDateTime, actualExam.CreatedDateTime);
    }

    [Fact]
    public void Map_FromEvaluationFinalizedEvent_ToEvalReceived_Succeeds()
    {
        var evaluationFinalizedEvent = new EvaluationFinalizedEvent
        {
            EvaluationId = 1
        };

        var expectedEvalReceived = new EvalReceived
        {
            EvaluationId = 1
        };

        var mapper = CreateMapper();

        // If not mapped, would throw AutoMapperMappingException
        var actual = mapper.Map<EvalReceived>(evaluationFinalizedEvent);

        Assert.Equal(expectedEvalReceived.EvaluationId, actual.EvaluationId);
    }

    [Theory]
    [MemberData(nameof(Map_FromEvaluationFinalizedEvent_ToEvalReceived_DateOfService_TestData))]
    public void Map_FromEvaluationFinalizedEvent_ToEvalReceived_DateOfService_Tests(DateTime? source, DateTime? expected)
    {
        var evaluationFinalizedEvent = new EvaluationFinalizedEvent
        {
            DateOfService = source
        };

        var mapper = CreateMapper();

        var evalReceived = mapper.Map<EvalReceived>(evaluationFinalizedEvent);

        Assert.Equal(expected, evalReceived.DateOfService);
    }

    public static IEnumerable<object[]> Map_FromEvaluationFinalizedEvent_ToEvalReceived_DateOfService_TestData()
    {
        yield return [null, null];

        yield return
        [
            new DateTime(2022, 05, 01, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2022, 05, 01, 0, 0, 0, DateTimeKind.Utc)
        ];

        yield return
        [
            new DateTime(2022, 05, 01, 0, 0, 0, DateTimeKind.Unspecified),
            new DateTime(2022, 05, 01, 0, 0, 0, DateTimeKind.Utc)
        ];

        // Don't hardcode this setting Kind to Local, because if the tests are run from a different time zone, they would fail
        var nowLocal = DateTime.Now;
        Assert.Equal(DateTimeKind.Local, nowLocal.Kind); // DateTime.Now sets the Kind to Local, but just in case for the sake of our tests

        yield return
        [
            nowLocal,
            nowLocal.ToUniversalTime()
        ];
    }

    [Fact]
    public void Map_FromExamResult_ToSpirometryExamResult_SetsSessionGrade()
    {
        var examResult = new ExamResult
        {
            SessionGrade = SessionGrade.B
        };

        var expected = new SpirometryExamResult
        {
            SessionGradeId = 2
        };

        var mapper = CreateMapper();

        var actual = mapper.Map<SpirometryExamResult>(examResult);

        Assert.Equal(expected.SessionGradeId, actual.SessionGradeId);

        // This is a FK, we don't want to set it in the mapper or it will try to insert as a new record into SessionGrade table
        Assert.Null(actual.SessionGrade);
    }

    [Fact]
    public void Map_FromRawExamResult_ToExamResult_Exists()
    {
        // If no mapping exists, AutoMapper will throw an AutoMapperMappingException with message
        // "Missing type map configuration or unsupported mapping"

        var source = new RawExamResult
        {
            Fev1 = "50" // Needs to be valid, because it is using the real validator
        };

        var mapper = CreateMapper();

        var actual = mapper.Map<ExamResult>(source);

        Assert.Equal(50, actual.Fev1);
    }

    [Fact]
    public void Map_FromExamResult_ToSpirometryExamResult_SetsAllTrileanTypes()
    {
        var examResult = new ExamResult
        {
            HasHighSymptom = TrileanType.Unknown,
            HasEnvOrExpRisk = TrileanType.Yes,
            HasHighComorbidity = TrileanType.No,
            HadWheezingPast12mo = TrileanType.Unknown,
            GetsShortnessOfBreathAtRest = TrileanType.Yes,
            GetsShortnessOfBreathWithMildExertion = TrileanType.No
        };

        var expected = new SpirometryExamResult
        {
            HasHighSymptomTrileanTypeId = 1,
            HasEnvOrExpRiskTrileanTypeId = 2,
            HasHighComorbidityTrileanTypeId = 3,
            HadWheezingPast12moTrileanTypeId = 1,
            GetsShortnessOfBreathAtRestTrileanTypeId = 2,
            GetsShortnessOfBreathWithMildExertionTrileanTypeId = 3
        };

        var mapper = CreateMapper();

        var actual = mapper.Map<SpirometryExamResult>(examResult);

        Assert.Equal(expected.HasHighSymptomTrileanTypeId, actual.HasHighSymptomTrileanTypeId);
        Assert.Equal(expected.HasEnvOrExpRiskTrileanTypeId, actual.HasEnvOrExpRiskTrileanTypeId);
        Assert.Equal(expected.HasHighComorbidityTrileanTypeId, actual.HasHighComorbidityTrileanTypeId);
        Assert.Equal(expected.HadWheezingPast12moTrileanTypeId, actual.HadWheezingPast12moTrileanTypeId);
        Assert.Equal(expected.GetsShortnessOfBreathAtRestTrileanTypeId, actual.GetsShortnessOfBreathAtRestTrileanTypeId);
        Assert.Equal(expected.GetsShortnessOfBreathWithMildExertionTrileanTypeId, actual.GetsShortnessOfBreathWithMildExertionTrileanTypeId);

        // These are FK, we don't want to set it in the mapper or it will try to insert as a new record into SessionGrade table
        Assert.Null(actual.HasHighSymptomTrileanType);
        Assert.Null(actual.HasEnvOrExpRiskTrileanType);
        Assert.Null(actual.HasHighComorbidityTrileanType);
        Assert.Null(actual.HadWheezingPast12moTrileanType);
        Assert.Null(actual.GetsShortnessOfBreathAtRestTrileanType);
        Assert.Null(actual.GetsShortnessOfBreathWithMildExertionTrileanType);
    }

    [Fact]
    public void Map_FromExamResult_ToSpirometryExamResult_SetsAllFrequencyTypes()
    {
        var examResult = new ExamResult
        {
            CoughMucusFrequency = OccurrenceFrequency.Rarely,
            NoisyChestFrequency = OccurrenceFrequency.Sometimes,
            ShortnessOfBreathPhysicalActivityFrequency = OccurrenceFrequency.Often
        };

        var expected = new SpirometryExamResult
        {
            CoughMucusOccurrenceFrequencyId = 2,
            NoisyChestOccurrenceFrequencyId = 3,
            ShortnessOfBreathPhysicalActivityOccurrenceFrequencyId = 4
        };

        var mapper = CreateMapper();

        var actual = mapper.Map<SpirometryExamResult>(examResult);

        Assert.Equal(expected.CoughMucusOccurrenceFrequencyId, actual.CoughMucusOccurrenceFrequencyId);
        Assert.Equal(expected.NoisyChestOccurrenceFrequencyId, actual.NoisyChestOccurrenceFrequencyId);
        Assert.Equal(expected.ShortnessOfBreathPhysicalActivityOccurrenceFrequencyId, actual.ShortnessOfBreathPhysicalActivityOccurrenceFrequencyId);

        // These are FK, we don't want to set it in the mapper or it will try to insert as a new record into OccurrenceFrequency table
        Assert.Null(actual.CoughMucusOccurrenceFrequency);
        Assert.Null(actual.NoisyChestOccurrenceFrequency);
        Assert.Null(actual.ShortnessOfBreathPhysicalActivityOccurrenceFrequency);
    }

    [Theory]
    [InlineData(NormalityIndicator.Undetermined, 1)]
    [InlineData(NormalityIndicator.Normal, 2)]
    [InlineData(NormalityIndicator.Abnormal, 3)]
    public void Map_FromExamResult_ToSpirometryExamResult_SetsCorrectNormality(NormalityIndicator indicator, short normalityIndicatorId)
    {
        var source = new ExamResult
        {
            NormalityIndicator = indicator
        };

        var mapper = CreateMapper();

        var destination = mapper.Map<SpirometryExamResult>(source);

        Assert.Equal(normalityIndicatorId, destination.NormalityIndicatorId); // NormalityIndicator PK

        // This is FK, we don't want to set it in the mapper or it will try to insert as a new record into Normality table
        Assert.Null(destination.NormalityIndicator);
    }

    [Fact]
    public void Map_FromExamResult_ToSpirometryExamResult_HandlesAllNormalityCases()
    {
        var mapper = CreateMapper();

        foreach (var indicator in Enum.GetValues<NormalityIndicator>())
        {
            // If not handled, would throw NotImplementedException
            var destination = mapper.Map<SpirometryExamResult>(new ExamResult { NormalityIndicator = indicator });

            // Also just verify it's been set
            Assert.NotEqual(0, destination.NormalityIndicatorId);
        }
    }

    [Theory]
    [MemberData(nameof(Map_FromExamResult_ToSpirometryExamResult_SetsResultValues_TestData))]
    public void Map_FromExamResult_ToSpirometryExamResult_SetsResultValues(ExamResult source, SpirometryExamResult expectedResult)
    {
        var mapper = CreateMapper();

        var actual = mapper.Map<SpirometryExamResult>(source);

        Assert.Equal(expectedResult.SessionGradeId, actual.SessionGradeId);
        Assert.Equal(expectedResult.Fvc, actual.Fvc);
        Assert.Equal(expectedResult.Fev1, actual.Fev1);
        Assert.Equal(expectedResult.Fev1FvcRatio, actual.Fev1FvcRatio);
        Assert.Equal(expectedResult.HasSmokedTobacco, actual.HasSmokedTobacco);
        Assert.Equal(expectedResult.TotalYearsSmoking, actual.TotalYearsSmoking);
        Assert.Equal(expectedResult.LungFunctionScore, actual.LungFunctionScore);

        // These are FK, we don't want to set it in the mapper or it will try to insert as a new record into SessionGrade table
        Assert.Null(actual.SessionGrade);
    }

    public static IEnumerable<object[]> Map_FromExamResult_ToSpirometryExamResult_SetsResultValues_TestData()
    {
        yield return
        [
            new ExamResult
            {
                SessionGrade = null,
                Fvc = null,
                Fev1 = null,
                Fev1FvcRatio = null,
                HasSmokedTobacco = null,
                TotalYearsSmoking = null,
                LungFunctionQuestionnaireScore = null
            },
            new SpirometryExamResult
            {
                SessionGradeId = null,
                Fvc = null,
                Fev1 = null,
                Fev1FvcRatio = null,
                HasSmokedTobacco = null,
                TotalYearsSmoking = null,
                LungFunctionScore = null
            }
        ];

        yield return
        [
            new ExamResult
            {
                SessionGrade = SessionGrade.B,
                Fvc = 50,
                Fev1 = 25,
                Fev1FvcRatio = 0.5m,
                HasSmokedTobacco = true,
                TotalYearsSmoking = 20,
                LungFunctionQuestionnaireScore = 34
            },
            new SpirometryExamResult
            {
                SessionGradeId = Signify.Spirometry.Core.Data.Entities.SessionGrade.B.SessionGradeId,
                Fvc = 50,
                Fev1 = 25,
                Fev1FvcRatio = 0.5m,
                HasSmokedTobacco = true,
                TotalYearsSmoking = 20,
                LungFunctionScore = 34
            }
        ];
    }

    [Fact]
    public void Map_FromSpirometryExam_ToExamNotPerformed_SetsSpirometryExamId()
    {
        const int spirometryExamId = 1;

        var source = new SpirometryExam
        {
            SpirometryExamId = spirometryExamId
        };

        var expected = new ExamNotPerformed
        {
            SpirometryExamId = spirometryExamId
        };

        var mapper = CreateMapper();

        var actual = mapper.Map<ExamNotPerformed>(source);

        Assert.Equal(expected, actual);
        Assert.Equal(spirometryExamId, actual.SpirometryExamId);
    }

    [Fact]
    public void Map_FromNotPerformedInfo_ToExamNotPerformed_HandlesAllNotPerformedReasonEnumerations()
    {
        var mapper = CreateMapper();

        foreach (var reason in Enum.GetValues<NotPerformedReason>())
        {
            // If reason not mapped, would throw NotImplementedException
            var result = mapper.Map<ExamNotPerformed>(new NotPerformedInfo(reason));

            // Also just verify it's been set
            Assert.NotEqual(0, result.NotPerformedReasonId);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("notes")]
    public void Map_FromNotPerformedInfo_ToExamNotPerformed_SetsNotes(string notes)
    {
        var mapper = CreateMapper();

        var result = mapper.Map<ExamNotPerformed>(new NotPerformedInfo(NotPerformedReason.NotInterested, notes));

        Assert.Equal(notes, result.Notes);
    }

    [Fact]
    public void Map_FromMemberInfo_ToSpirometryExam_SetsAllRelatedProperties()
    {
        var memberInfo = new MemberInfo
        {
            MemberId = 1,
            FirstName = "FirstName",
            MiddleName = "MiddleName",
            LastName = "LastName",
            DateOfBirth = new DateTime(2020, 01, 01, 0, 0, 0, DateTimeKind.Utc),
            AddressLineOne = "A1",
            AddressLineTwo = "A2",
            City = "city",
            State = "TX",
            ZipCode = "55555"
        };

        var expected = new SpirometryExam
        {
            MemberId = memberInfo.MemberId,
            FirstName = memberInfo.FirstName,
            MiddleName = memberInfo.MiddleName,
            LastName = memberInfo.LastName,
            DateOfBirth = memberInfo.DateOfBirth,
            AddressLineOne = memberInfo.AddressLineOne,
            AddressLineTwo = memberInfo.AddressLineTwo,
            City = memberInfo.City,
            State = memberInfo.State,
            ZipCode = memberInfo.ZipCode
        };

        var mapper = CreateMapper();

        var actual = mapper.Map<SpirometryExam>(memberInfo);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Map_FromSpirometryExam_ToCreateBillRequest_Exists()
    {
        // If no mapping exists, AutoMapper will throw an AutoMapperMappingException with message
        // "Missing type map configuration or unsupported mapping"

        var source = new SpirometryExam();

        var mapper = CreateMapper();

        var actual = mapper.Map<CreateBillRequest>(source);

        Assert.NotNull(actual);
    }

    [Fact]
    public void Map_FromCreateBill_ToCreateBillRequest_Exists()
    {
        // If no mapping exists, AutoMapper will throw an AutoMapperMappingException with message
        // "Missing type map configuration or unsupported mapping"

        var source = new CreateBill();

        var mapper = CreateMapper();

        var actual = mapper.Map<CreateBillRequest>(source);

        Assert.NotNull(actual);
    }

    [Fact]
    public void Map_FromExamStatusEvent_ToExamStatus_Tests()
    {
        var source = new ExamStatusEvent
        {
            EventId = Guid.NewGuid(),
            StatusCode = StatusCode.BillRequestSent,
            Exam = new SpirometryExam
            {
                SpirometryExamId = 2
            },
            StatusDateTime = DateTime.UtcNow
        };

        var mapper = CreateMapper();

        var actual = mapper.Map<ExamStatus>(source);

        Assert.Equal(Signify.Spirometry.Core.Data.Entities.StatusCode.BillRequestSent.StatusCodeId, actual.StatusCodeId);
        Assert.Equal(source.StatusDateTime, actual.StatusDateTime);
        Assert.Equal(source.Exam.SpirometryExamId, actual.SpirometryExamId);

        Assert.Null(actual.SpirometryExam);
        Assert.Null(actual.StatusCode);
    }

    [Fact]
    public void Map_FromExam_ToStatusPerformed_Tests()
    {
        var source = new SpirometryExam
        {
            EvaluationCreatedDateTime = DateTime.Now,
            EvaluationReceivedDateTime = DateTime.UtcNow
        };

        var mapper = CreateMapper();

        var actual = mapper.Map<Performed>(source);

        Assert.Equal(Constants.ProductCodes.Spirometry, actual.ProductCode);
        Assert.Equal(source.EvaluationCreatedDateTime, actual.CreateDate);
        Assert.Equal(source.EvaluationReceivedDateTime, actual.ReceivedDate);
    }

    [Fact]
    public void Map_FromExam_ToStatusNotPerformed_Tests()
    {
        var source = new SpirometryExam
        {
            EvaluationCreatedDateTime = DateTime.Now,
            EvaluationReceivedDateTime = DateTime.UtcNow
        };

        var mapper = CreateMapper();

        var actual = mapper.Map<NotPerformed>(source);

        Assert.Equal(Constants.ProductCodes.Spirometry, actual.ProductCode);
        Assert.Equal(source.EvaluationCreatedDateTime, actual.CreateDate);
        Assert.Equal(source.EvaluationReceivedDateTime, actual.ReceivedDate);
    }

    [Fact]
    public void Map_FromExam_ToStatusFlaggedForLoopback_Tests()
    {
        var source = new SpirometryExam
        {
            EvaluationCreatedDateTime = DateTime.Now,
            EvaluationReceivedDateTime = DateTime.UtcNow
        };

        var mapper = CreateMapper();

        var actual = mapper.Map<FlaggedForLoopback>(source);

        Assert.Equal(Constants.ProductCodes.Spirometry, actual.ProductCode);
        Assert.Equal(source.EvaluationCreatedDateTime, actual.CreateDate);
        Assert.Equal(source.EvaluationReceivedDateTime, actual.ReceivedDate);
    }

    [Fact]
    public void Map_FromExam_ToStatusBillRequestSent_Tests()
    {
        var source = new SpirometryExam
        {
            EvaluationCreatedDateTime = DateTime.Now,
            EvaluationReceivedDateTime = DateTime.UtcNow
        };

        var mapper = CreateMapper();

        var actual = mapper.Map<Signify.Spirometry.Core.Events.Status.BillRequestSent>(source);

        Assert.Equal(Constants.ProductCodes.Spirometry, actual.ProductCode);
        Assert.Equal(source.EvaluationCreatedDateTime, actual.CreateDate);
        Assert.Equal(Constants.ProductCodes.Spirometry, actual.BillingProductCode);
    }

    [Fact]
    public void Map_FromExam_ToStatusBillRequestNotSent_Tests()
    {
        var source = new SpirometryExam
        {
            EvaluationCreatedDateTime = DateTime.Now,
            EvaluationReceivedDateTime = DateTime.UtcNow
        };

        var mapper = CreateMapper();

        var actual = mapper.Map<BillRequestNotSent>(source);

        Assert.Equal(Constants.ProductCodes.Spirometry, actual.ProductCode);
        Assert.Equal(source.EvaluationCreatedDateTime, actual.CreateDate);
        Assert.Equal(source.EvaluationReceivedDateTime, actual.ReceivedDate);
    }

    [Fact]
    public void Map_FromExam_ToStatusResultsReceived_Exists()
    {
        // If no mapping exists, AutoMapper will throw an AutoMapperMappingException with message
        // "Missing type map configuration or unsupported mapping"

        var source = new SpirometryExam
        {
            EvaluationCreatedDateTime = DateTime.Now,
            EvaluationReceivedDateTime = DateTime.UtcNow
        };

        var mapper = CreateMapper();

        var actual = mapper.Map<Core.Events.Status.ResultsReceived>(source);

        Assert.NotNull(actual);
        Assert.Equal(Constants.ProductCodes.Spirometry, actual.ProductCode);
        Assert.Equal(source.EvaluationCreatedDateTime, actual.CreateDate);
        Assert.Equal(source.EvaluationReceivedDateTime, actual.ReceivedDate);
    }

    [Fact]
    public void Map_FromExamNotPerformed_ToStatusNotPerformed_Tests()
    {
        var source = new ExamNotPerformed
        {
            NotPerformedReason = new Signify.Spirometry.Core.Data.Entities.NotPerformedReason(1, 2, "Test"),
            Notes = "TestNotes"
        };

        var mapper = CreateMapper();

        var actual = mapper.Map<NotPerformed>(source);

        Assert.Equal(source.NotPerformedReason.Reason, actual.Reason);
        Assert.Equal(source.Notes, actual.ReasonNotes);
    }

    [Fact]
    public void Map_FromPdf_ToStatusBillRequestNotSent_Tests()
    {
        var source = new PdfDeliveredToClient
        {
            PdfDeliveredToClientId = 1,
            DeliveryDateTime = DateTime.UtcNow
        };

        var mapper = CreateMapper();

        var actual = mapper.Map<BillRequestNotSent>(source);

        Assert.Equal(source.DeliveryDateTime, actual.PdfDeliveryDate);
    }

    [Fact]
    public void Map_FromBillRequestSent_ToStatusBillRequestSent_Tests()
    {
        var source = new Signify.Spirometry.Core.Data.Entities.BillRequestSent
        {
            BillId = Guid.NewGuid(),
        };

        var mapper = CreateMapper();

        var actual = mapper.Map<Signify.Spirometry.Core.Events.Status.BillRequestSent>(source);

        Assert.Equal(source.BillId, actual.BillId);
    }

    [Fact]
    public void Map_FromOverreadProcessed_ToOverreadResult_Tests()
    {
        // Arrange
        var now = DateTimeOffset.Now;
        var i = 0;

        DateTimeOffset GetNewTimestamp()
            => now.AddHours(++i);

        var source = new OverreadProcessed
        {
            OverreadId = Guid.NewGuid(),
            Comment = "comment",
            PerformedDateTime = GetNewTimestamp(),
            OverreadDateTime = GetNewTimestamp(),
            ReceivedDateTime = GetNewTimestamp()
        };

        // Act
        var mapper = CreateMapper();

        var actual = mapper.Map<OverreadResult>(source);

        // Assert
        Assert.Equal(source.OverreadId, actual.ExternalId);
        Assert.Equal(source.Comment, actual.OverreadComment);

        Assert.Equal(actual.PerformedDateTime, source.PerformedDateTime);
        Assert.Equal(actual.OverreadDateTime, source.OverreadDateTime);
        Assert.Equal(actual.ReceivedDateTime, source.ReceivedDateTime);
    }

    [Fact]
    public void Map_FromSpirometryExam_ToResultsReceived_Exists()
    {
        // If no mapping exists, AutoMapper will throw an AutoMapperMappingException with message
        // "Missing type map configuration or unsupported mapping"

        var source = new SpirometryExam();

        var mapper = CreateMapper();

        var actual = mapper.Map<Core.Events.Akka.ResultsReceived>(source);

        Assert.NotNull(actual);
    }

    [Fact]
    public void Map_FromSpirometryExamResult_ToResultsReceived_Exists()
    {
        // If no mapping exists, AutoMapper will throw an AutoMapperMappingException with message
        // "Missing type map configuration or unsupported mapping"

        var source = new SpirometryExamResult();

        var mapper = CreateMapper();

        var actual = mapper.Map<Core.Events.Akka.ResultsReceived>(source);

        Assert.NotNull(actual);
    }

    [Fact]
    public void Map_FromSpirometryExam_ToSaveSystemFlagRequest_Exists()
    {
        // If no mapping exists, AutoMapper will throw an AutoMapperMappingException with message
        // "Missing type map configuration or unsupported mapping"

        var source = new SpirometryExam();

        var mapper = CreateMapper();

        var actual = mapper.Map<SaveSystemFlagRequest>(source);

        Assert.NotNull(actual);
    }

    [Fact]
    public void Map_FromSpirometryExamResult_ToSaveSystemFlagRequest_Exists()
    {
        // If no mapping exists, AutoMapper will throw an AutoMapperMappingException with message
        // "Missing type map configuration or unsupported mapping"

        var source = new SpirometryExamResult();

        var mapper = CreateMapper();

        var actual = mapper.Map<SaveSystemFlagRequest>(source);

        Assert.NotNull(actual);
    }

    [Fact]
    public void Map_From_CdiEventForPayment_To_SaveProviderPay()
    {
        // If no mapping exists, AutoMapper will throw an AutoMapperMappingException with message
        // "Missing type map configuration or unsupported mapping"

        var source = new CdiEventForPayment
        {
            EvaluationId = 123456,
            ApplicationId = "Test",
            EventType = "CDIFailedEvent",
            PayProvider = true,
            RequestId = Guid.NewGuid(),
            DateTime = new FakeApplicationTime().UtcNow().AddDays(-1),
            Reason = "Expired kits used",
            CdiEventForPaymentId = 1234,
            CreatedDateTime = new FakeApplicationTime().UtcNow()
        };

        var mapper = CreateMapper();

        var actual = mapper.Map<SaveProviderPay>(source);

        Assert.NotNull(actual);
        Assert.Equal(source.RequestId, actual.EventId);
        Assert.Equal(source.CreatedDateTime, actual.ParentEventReceivedDateTime);
        Assert.Equal(source.DateTime, actual.ParentEventDateTime);
        Assert.Equal(source.EvaluationId, actual.EvaluationId);
        Assert.Equal(source.RequestId, actual.EventId);
        Assert.Equal(0, actual.ExamId);
    }

    [Fact]
    public void Map_From_SaveProviderPay_To_ProviderPay()
    {
        // If no mapping exists, AutoMapper will throw an AutoMapperMappingException with message
        // "Missing type map configuration or unsupported mapping"

        var source = new SaveProviderPay
        {
            EventId = Guid.NewGuid(),
            EvaluationId = 123456,
            PaymentId = Guid.NewGuid().ToString(),
            ExamId = 1001,
            ParentEventDateTime = new FakeApplicationTime().UtcNow().AddDays(-1),
            ParentEventReceivedDateTime = new FakeApplicationTime().UtcNow()
        };

        var mapper = CreateMapper();

        var actual = mapper.Map<ProviderPay>(source);

        Assert.NotNull(actual);
        Assert.Equal(source.PaymentId, actual.PaymentId);
        Assert.Equal(source.ExamId, actual.SpirometryExamId);
    }

    [Fact]
    public void Map_From_SpirometryExam_To_ProviderPayRequestSent()
    {
        // If no mapping exists, AutoMapper will throw an AutoMapperMappingException with message
        // "Missing type map configuration or unsupported mapping"

        var source = new SpirometryExam
        {
            EvaluationId = 123456,
            SpirometryExamId = 1001,
            MemberPlanId = 654321,
            ProviderId = 4321,
            EvaluationCreatedDateTime = new FakeApplicationTime().UtcNow().AddDays(-2),
            EvaluationReceivedDateTime = new FakeApplicationTime().UtcNow().AddDays(-1),
            CreatedDateTime = new FakeApplicationTime().UtcNow()
        };

        var mapper = CreateMapper();

        var actual = mapper.Map<ProviderPayRequestSent>(source);

        Assert.NotNull(actual);
        Assert.Equal(source.EvaluationId, actual.EvaluationId);
        Assert.NotEqual(source.CreatedDateTime, actual.CreateDate);
        Assert.NotEqual(source.EvaluationReceivedDateTime, actual.ReceivedDate);
        Assert.Equal("SPIROMETRY", actual.ProviderPayProductCode);
        Assert.Equal("SPIROMETRY", actual.ProductCode);
        Assert.Equal(source.MemberPlanId, actual.MemberPlanId);
        Assert.Equal(source.ProviderId, actual.ProviderId);
    }

    [Fact]
    public void Map_From_SpirometryExam_To_ProviderPayableEventReceived()
    {
        // If no mapping exists, AutoMapper will throw an AutoMapperMappingException with message
        // "Missing type map configuration or unsupported mapping"

        var source = new SpirometryExam
        {
            EvaluationId = 123456,
            SpirometryExamId = 1001,
            MemberPlanId = 654321,
            ProviderId = 4321,
            EvaluationCreatedDateTime = new FakeApplicationTime().UtcNow().AddDays(-2),
            EvaluationReceivedDateTime = new FakeApplicationTime().UtcNow().AddDays(-1),
            CreatedDateTime = new FakeApplicationTime().UtcNow()
        };

        var mapper = CreateMapper();

        var actual = mapper.Map<ProviderPayableEventReceived>(source);

        Assert.NotNull(actual);
        Assert.Equal(source.EvaluationId, actual.EvaluationId);
        Assert.NotEqual(source.CreatedDateTime, actual.CreateDate);
        Assert.NotEqual(source.EvaluationReceivedDateTime, actual.ReceivedDate);
        Assert.Equal("SPIROMETRY", actual.ProductCode);
        Assert.Equal(source.MemberPlanId, actual.MemberPlanId);
        Assert.Equal(source.ProviderId, actual.ProviderId);
        Assert.Null(actual.ParentCdiEvent);
    }

    [Fact]
    public void Map_From_SpirometryExam_To_ProviderNonPayableEventReceived()
    {
        // If no mapping exists, AutoMapper will throw an AutoMapperMappingException with message
        // "Missing type map configuration or unsupported mapping"

        var source = new SpirometryExam
        {
            EvaluationId = 123456,
            SpirometryExamId = 1001,
            MemberPlanId = 654321,
            ProviderId = 4321,
            EvaluationCreatedDateTime = new FakeApplicationTime().UtcNow().AddDays(-2),
            EvaluationReceivedDateTime = new FakeApplicationTime().UtcNow().AddDays(-1),
            CreatedDateTime = new FakeApplicationTime().UtcNow()
        };

        var mapper = CreateMapper();

        var actual = mapper.Map<ProviderNonPayableEventReceived>(source);

        Assert.NotNull(actual);
        Assert.Equal(source.EvaluationId, actual.EvaluationId);
        Assert.NotEqual(source.CreatedDateTime, actual.CreateDate);
        Assert.NotEqual(source.EvaluationReceivedDateTime, actual.ReceivedDate);
        Assert.Equal("SPIROMETRY", actual.ProductCode);
        Assert.Equal(source.MemberPlanId, actual.MemberPlanId);
        Assert.Equal(source.ProviderId, actual.ProviderId);
        Assert.Null(actual.ParentCdiEvent);
        Assert.Null(actual.Reason);
    }

    [Fact]
    public void Map_From_SpirometryExam_To_ProviderPayApiRequest()
    {
        // If no mapping exists, AutoMapper will throw an AutoMapperMappingException with message
        // "Missing type map configuration or unsupported mapping"

        var source = new SpirometryExam
        {
            EvaluationId = 123456,
            SpirometryExamId = 1001,
            MemberPlanId = 654321,
            ClientId = 9090,
            ProviderId = 4321,
            CenseoId = "X12345",
            DateOfService = new DateTime(2023, 10, 30, 0, 0, 0, DateTimeKind.Local),
            EvaluationCreatedDateTime = new FakeApplicationTime().UtcNow().AddDays(-4),
            EvaluationReceivedDateTime = new FakeApplicationTime().UtcNow().AddDays(-1),
            CreatedDateTime = new FakeApplicationTime().UtcNow()
        };

        var mapper = CreateMapper();

        var actual = mapper.Map<ProviderPayApiRequest>(source);

        Assert.NotNull(actual);
        Assert.Equal("SPIROMETRY", actual.ProviderProductCode);
        Assert.Equal(source.ClientId, actual.ClientId);
        Assert.Equal(source.ProviderId, actual.ProviderId);
        Assert.Equal(source.CenseoId, actual.PersonId);
        Assert.Equal("2023-10-30", actual.DateOfService);
    }

    [Fact]
    public void Map_From_ExamStatusEvent_To_ProviderPayRequestSent()
    {
        // If no mapping exists, AutoMapper will throw an AutoMapperMappingException with message
        // "Missing type map configuration or unsupported mapping"

        var source = new ExamStatusEvent
        {
            PaymentId = Guid.NewGuid().ToString(),
            StatusDateTime = new FakeApplicationTime().UtcNow().AddDays(-2),
            ParentEventReceivedDateTime = new FakeApplicationTime().UtcNow().AddDays(-1)
        };

        var mapper = CreateMapper();

        var actual = mapper.Map<ProviderPayRequestSent>(source);

        Assert.NotNull(actual);
        Assert.Equal(source.ParentEventReceivedDateTime, actual.CreateDate);
        Assert.Equal(source.ParentEventReceivedDateTime, actual.ReceivedDate);
        Assert.Equal(source.StatusDateTime, actual.ParentEventDateTime);
        Assert.Equal("SPIROMETRY", actual.ProviderPayProductCode);
        Assert.Equal("SPIROMETRY", actual.ProductCode);
        Assert.Equal(source.PaymentId, actual.PaymentId);
    }

    [Theory]
    [InlineData(nameof(CDIPassedEvent))]
    [InlineData(nameof(CDIFailedEvent))]
    public void Map_From_ExamStatusEvent_To_ProviderNonPayableEventReceived(string cdiEvent)
    {
        // If no mapping exists, AutoMapper will throw an AutoMapperMappingException with message
        // "Missing type map configuration or unsupported mapping"

        var source = new ExamStatusEvent
        {
            PaymentId = Guid.NewGuid().ToString(),
            StatusDateTime = new FakeApplicationTime().UtcNow().AddDays(-2),
            ParentEventReceivedDateTime = new FakeApplicationTime().UtcNow().AddDays(-1),
            ParentCdiEvent = cdiEvent,
            Reason = "Dummy reason for test"
        };

        var mapper = CreateMapper();

        var actual = mapper.Map<ProviderNonPayableEventReceived>(source);

        Assert.NotNull(actual);
        Assert.Equal(source.ParentEventReceivedDateTime, actual.CreateDate);
        Assert.Equal(source.ParentEventReceivedDateTime, actual.ReceivedDate);
        Assert.Equal("SPIROMETRY", actual.ProductCode);
        Assert.Equal(actual.Reason, actual.Reason);
        Assert.Equal(actual.ParentCdiEvent, actual.ParentCdiEvent);
    }

    [Theory]
    [InlineData(nameof(CDIPassedEvent))]
    [InlineData(nameof(CDIFailedEvent))]
    public void Map_From_ExamStatusEvent_To_ProviderPayableEventReceived(string cdiEvent)
    {
        // If no mapping exists, AutoMapper will throw an AutoMapperMappingException with message
        // "Missing type map configuration or unsupported mapping"

        var source = new ExamStatusEvent
        {
            PaymentId = Guid.NewGuid().ToString(),
            StatusDateTime = new FakeApplicationTime().UtcNow().AddDays(-2),
            ParentEventReceivedDateTime = new FakeApplicationTime().UtcNow().AddDays(-1),
            ParentCdiEvent = cdiEvent,
            Reason = "Dummy reason for test"
        };

        var mapper = CreateMapper();

        var actual = mapper.Map<ProviderPayableEventReceived>(source);

        Assert.NotNull(actual);
        Assert.Equal(source.ParentEventReceivedDateTime, actual.CreateDate);
        Assert.Equal(source.ParentEventReceivedDateTime, actual.ReceivedDate);
        Assert.Equal("SPIROMETRY", actual.ProductCode);
        Assert.Equal(actual.ParentCdiEvent, actual.ParentCdiEvent);
    }

    [Fact]
    public void Map_From_CDIPassedEvent_To_CdiEvent()
    {
        // If no mapping exists, AutoMapper will throw an AutoMapperMappingException with message
        // "Missing type map configuration or unsupported mapping"

        var source = new CDIPassedEvent
        {
            RequestId = Guid.NewGuid(),
            DateTime = new FakeApplicationTime().UtcNow().AddDays(-2),
            EvaluationId = 123456,
            ApplicationId = "AppId",
            ReceivedBySpiroDateTime = new FakeApplicationTime().UtcNow()
        };

        var mapper = CreateMapper();

        var actual = mapper.Map<CdiEventForPayment>(source);

        Assert.NotNull(actual);
        Assert.Equal(source.RequestId, actual.RequestId);
        Assert.Equal(source.DateTime, actual.DateTime);
        Assert.Equal(source.EvaluationId, actual.EvaluationId);
        Assert.Equal(source.ApplicationId, actual.ApplicationId);
        Assert.Equal("CDIPassedEvent", actual.EventType);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Map_From_CDIFailedEvent_To_CdiEvent(bool payProvider)
    {
        // If no mapping exists, AutoMapper will throw an AutoMapperMappingException with message
        // "Missing type map configuration or unsupported mapping"

        var source = new CDIFailedEvent
        {
            RequestId = Guid.NewGuid(),
            DateTime = new FakeApplicationTime().UtcNow().AddDays(-2),
            EvaluationId = 123456,
            ApplicationId = "AppId",
            ReceivedBySpiroDateTime = new FakeApplicationTime().UtcNow(),
            PayProvider = payProvider,
            Reason = "Expired kits"
        };

        var mapper = CreateMapper();

        var actual = mapper.Map<CdiEventForPayment>(source);

        Assert.NotNull(actual);
        Assert.Equal(source.RequestId, actual.RequestId);
        Assert.Equal(source.DateTime, actual.DateTime);
        Assert.Equal(source.EvaluationId, actual.EvaluationId);
        Assert.Equal(source.ApplicationId, actual.ApplicationId);
        Assert.Equal("CDIFailedEvent", actual.EventType);
    }

    [Theory]
    [InlineData(true, "CDIPassedEvent")]
    [InlineData(true, "CDIFailedEvent")]
    [InlineData(false, "CDIFailedEvent")]
    public void Map_From_CdiEvent_To_CdiReceivedEvent(bool payProvider, string eventType)
    {
        // If no mapping exists, AutoMapper will throw an AutoMapperMappingException with message
        // "Missing type map configuration or unsupported mapping"

        var source = new CdiEventForPayment
        {
            RequestId = Guid.NewGuid(),
            DateTime = new FakeApplicationTime().UtcNow().AddDays(-2),
            EvaluationId = 123456,
            ApplicationId = "AppId",
            CreatedDateTime = new FakeApplicationTime().UtcNow(),
            Reason = "Dummy reason",
            EventType = eventType,
            PayProvider = payProvider
        };

        var mapper = CreateMapper();

        var actual = mapper.Map<CdiEventForPaymentReceived>(source);

        Assert.NotNull(actual);
        Assert.Equal(source.EvaluationId, actual.EvaluationId);
        Assert.Equal(source.CreatedDateTime, actual.CreatedDateTime);
    }

    [Fact]
    public void Map_From_CdiEventForPayment_To_ExamStatusEvent()
    {
        // If no mapping exists, AutoMapper will throw an AutoMapperMappingException with message
        // "Missing type map configuration or unsupported mapping"

        var source = new CdiEventForPayment
        {
            RequestId = Guid.NewGuid(),
            DateTime = new FakeApplicationTime().UtcNow().AddDays(-2),
            EvaluationId = 123456,
            ApplicationId = "AppId",
            CreatedDateTime = new FakeApplicationTime().UtcNow(),
            Reason = "Dummy reason",
            EventType = "CDIFailedEvent",
            PayProvider = true
        };

        var mapper = CreateMapper();

        var actual = mapper.Map<ExamStatusEvent>(source);

        Assert.NotNull(actual);
        Assert.Equal(source.RequestId, actual.EventId);
        Assert.Equal(source.DateTime.UtcDateTime, actual.StatusDateTime);
        Assert.Equal(source.EventType, actual.ParentCdiEvent);
        Assert.Equal(source.CreatedDateTime, actual.ParentEventReceivedDateTime);
    }

    [Theory]
    [MemberData(nameof(DateTimeOffsetInDifferentTimeZone))]
    public void Map_From_CdiPassedEvent_To_CdiEventForPayment(DateTimeOffset dateTime)
    {
        // If no mapping exists, AutoMapper will throw an AutoMapperMappingException with message
        // "Missing type map configuration or unsupported mapping"

        var source = new CDIPassedEvent
        {
            RequestId = Guid.NewGuid(),
            DateTime = dateTime,
            EvaluationId = 123456,
            ApplicationId = "AppId"
        };

        var mapper = CreateMapper();

        var actual = mapper.Map<CdiEventForPayment>(source);

        Assert.NotNull(actual);
        Assert.Equal(source.RequestId, actual.RequestId);
        Assert.Equal(source.DateTime.UtcDateTime, actual.DateTime);
        Assert.Equal(source.EvaluationId, actual.EvaluationId);
        Assert.Equal(source.ApplicationId, actual.ApplicationId);
        Assert.Equal("CDIPassedEvent", actual.EventType);
    }

    [Theory]
    [MemberData(nameof(DateTimeOffsetInDifferentTimeZone))]
    public void Map_From_CdiFailedEvent_To_CdiEventForPayment(DateTimeOffset dateTime)
    {
        // If no mapping exists, AutoMapper will throw an AutoMapperMappingException with message
        // "Missing type map configuration or unsupported mapping"

        var source = new CDIFailedEvent
        {
            RequestId = Guid.NewGuid(),
            DateTime = dateTime,
            EvaluationId = 123456,
            ApplicationId = "AppId",
            Reason = "Dummy reason",
            PayProvider = true
        };

        var mapper = CreateMapper();

        var actual = mapper.Map<CdiEventForPayment>(source);

        Assert.NotNull(actual);
        Assert.Equal(source.RequestId, actual.RequestId);
        Assert.Equal(source.DateTime.UtcDateTime, actual.DateTime);
        Assert.Equal(source.EvaluationId, actual.EvaluationId);
        Assert.Equal(source.ApplicationId, actual.ApplicationId);
        Assert.Equal(source.Reason, actual.Reason);
        Assert.Equal(source.PayProvider, actual.PayProvider);
        Assert.Equal("CDIFailedEvent", actual.EventType);
    }

    public static IEnumerable<object[]> DateTimeOffsetInDifferentTimeZone()
    {
        yield return
        [
            new DateTimeOffset(2022, 05, 01, 7, 8, 9, TimeSpan.Zero)
        ];
        yield return
        [
            new DateTimeOffset(2022, 05, 01, 1, 2, 3, new TimeSpan(1, 0, 0))
        ];
        yield return
        [
            new DateTimeOffset(2022, 05, 01, 0, 0, 0, new TimeSpan(-5, 0, 0))
        ];
        yield return
        [
            new DateTimeOffset(2022, 05, 01, 7, 8, 9, new TimeSpan(-5, 0, 0))
        ];
    }
}