using AutoMapper;
using EgfrNsbEvents;
using Signify.eGFR.Core.ApiClients.MemberApi.Responses;
using Signify.eGFR.Core.Data.Entities;
using Signify.eGFR.Core.Events.Akka;
using Signify.eGFR.Core.Events;
using Signify.eGFR.Core.Infrastructure;
using Signify.eGFR.Core.Maps;
using System.Collections.Generic;
using System;
using Xunit;

using NotPerformedReason = Signify.eGFR.Core.Models.NotPerformedReason;

namespace Signify.eGFR.Core.Tests.Maps;

public class MappingProfileTests
{
    private readonly IApplicationTime _applicationTime = new FakeApplicationTime();

    private static IMapper CreateMapper()
        => new MapperConfiguration(configure => { configure.AddProfile<MappingProfile>(); }).CreateMapper();

    [Fact]
    public void Map_FromEvalReceived_To_Exam_MapsAllProperties()
    {
        var date = new DateTimeOffset(2020, 01, 01, 01, 01, 01, new TimeSpan(0,0,0));

        const int evaluationId = 1;
        const int providerId = 2;
        const long memberId = 3;
        const int memberPlanId = 4;
        var receivedDateTime = date.AddMinutes(1);
        var receivedByProcessManager = receivedDateTime.AddMinutes(1);

        var evalReceived = new EvalReceived
        {
            ApplicationId = nameof(ApplicationId),
            EvaluationId = evaluationId,
            ProviderId = providerId,
            MemberId = memberId,
            MemberPlanId = memberPlanId,
            CreatedDateTime = date,
            ReceivedDateTime = receivedDateTime,
            DateOfService = date,
            ReceivedByeGFRProcessManagerDateTime = receivedByProcessManager
        };

        var expectedExam = new Exam
        {
            ApplicationId = nameof(ApplicationId),
            EvaluationId = evaluationId,
            ProviderId = providerId,
            MemberId = memberId,
            MemberPlanId = memberPlanId,
            EvaluationCreatedDateTime = date,
            EvaluationReceivedDateTime = receivedDateTime,
            DateOfService = date,
            CreatedDateTime = receivedByProcessManager
        };

        var mapper = CreateMapper();

        var actualExam = mapper.Map<Exam>(evalReceived);

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
    public void Map_FromEvaluationFinalizedEvent_ToEvalReceived_DateOfService_Tests(DateTimeOffset? source, DateTimeOffset? expected)
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
            new DateTimeOffset(2022, 05, 01, 0, 0, 0, new TimeSpan(0, 0, 0)),
            new DateTimeOffset(2022, 05, 01, 0, 0, 0, new TimeSpan(0, 0, 0))
        ];

        yield return
        [
            new DateTimeOffset(2022, 05, 01, 0, 0, 0, new TimeSpan(0, 0, 0)),
            new DateTimeOffset(2022, 05, 01, 0, 0, 0, new TimeSpan(0, 0, 0))
        ];
    }

    [Fact]
    public void Map_FromMemberInfo_ToExam_SetsAllRelatedProperties()
    {
        var memberInfo = new MemberInfo
        {
            MemberId = 1,
            FirstName = "FirstName",
            MiddleName = "MiddleName",
            LastName = "LastName",
            DateOfBirth = new DateTime(2020, 01, 01),
            AddressLineOne = "A1",
            AddressLineTwo = "A2",
            City = "city",
            State = "TX",
            ZipCode = "55555"
        };

        var expected = new Exam
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

        var actual = mapper.Map<Exam>(memberInfo);

        Assert.Equal(expected.MemberId, actual.MemberId);
        Assert.Equal(expected.FirstName, actual.FirstName);
        Assert.Equal(expected.MiddleName, actual.MiddleName);
        Assert.Equal(expected.LastName, actual.LastName);
        Assert.Equal(expected.DateOfBirth, actual.DateOfBirth);
        Assert.Equal(expected.AddressLineOne, actual.AddressLineOne);
        Assert.Equal(expected.AddressLineTwo, actual.AddressLineTwo);
        Assert.Equal(expected.City, actual.City);
        Assert.Equal(expected.State, actual.State);
        Assert.Equal(expected.ZipCode, actual.ZipCode);
    }

    [Fact]
    public void Map_FromExamStatusEvent_ToExamStatus_Tests()
    {
        var source = new ExamStatusEvent
        {
            EventId = Guid.NewGuid(),
            EvaluationId = 1,
            StatusCode = ExamStatusCode.ExamPerformed,
            ExamId = 2,
            StatusDateTime = DateTime.UtcNow
        };

        var mapper = CreateMapper();

        var actual = mapper.Map<ExamStatus>(source);

        Assert.Equal(ExamStatusCode.ExamPerformed.StatusCodeId, actual.ExamStatusCodeId);
        Assert.Equal(source.StatusDateTime, actual.StatusDateTime);
        Assert.Equal(source.ExamId, actual.ExamId);
        Assert.Null(actual.Exam);
        Assert.Null(actual.ExamStatusCode);
    }
        
    [Fact]
    public void Map_FromLabResultEvent_ToLabResult_Tests()
    {
        var source = new EgfrLabResult
        {
            CenseoId = "1",
            VendorLabTestId = 123,
            VendorLabTestNumber = "abc",
            eGFRResult = 1,
            CreatinineResult = 12,
            MailDate = DateTimeOffset.UtcNow,
            CollectionDate = DateTimeOffset.UtcNow,
            AccessionedDate = DateTimeOffset.UtcNow
        };

        var mapper = CreateMapper();
        var actual = mapper.Map<QuestLabResult>(source);
        Assert.Equal(source.CenseoId, actual.CenseoId);
        Assert.Equal(source.VendorLabTestId, actual.VendorLabTestId);
        Assert.Equal(source.VendorLabTestNumber, actual.VendorLabTestNumber);
        Assert.Equal(source.eGFRResult, actual.eGFRResult);
        Assert.Equal(source.CreatinineResult, actual.CreatinineResult);
        Assert.Equal(source.MailDate, actual.MailDate);
        Assert.Equal(source.CollectionDate, actual.CollectionDate);
        Assert.Equal(source.AccessionedDate, actual.AccessionedDate);
    }
        
    [Fact]
    public void Map_FromExam_ToResultsReceived_Tests()
    {
        var destination = new ResultsReceived();

        var mapper = CreateMapper();
        var actual = mapper.Map<ResultsReceived>(new Exam());

        Assert.Equal(destination.EvaluationId, actual.EvaluationId);
        Assert.Null(actual.Result);
    }
        
    [Fact]
    public void Map_FromQuestLabResult_Test_ToResultsReceived_Tests()
    {
        var destination = new ResultsReceived();
        var source = new QuestLabResult
        {
            NormalityCode = "A"
        };

        var mapper = CreateMapper();
        var actual = mapper.Map<ResultsReceived>(source);

        Assert.Equal(destination.EvaluationId, actual.EvaluationId);
        Assert.Equal(source.NormalityCode, actual.Determination);
        Assert.Equal(source.NormalityCode, actual.Result.AbnormalIndicator);
    }
    
    [Theory]
    [InlineData(1, "U", "unknown")]
    [InlineData(2, "N", "")]
    [InlineData(3, "A", null)]
    [InlineData(6, "U", "ABC")]
    [InlineData(0, "U", "ABC")]
    public void Map_FromLabResult_Test_ToResultsReceived_Tests(int normalityIndicator, string expectedNormality, string description)
    {
        var destination = new ResultsReceived();
        var source = new LabResult
        {
            NormalityIndicatorId = normalityIndicator,
            ResultDescription = description
        };

        var mapper = CreateMapper();
        var actual = mapper.Map<ResultsReceived>(source);

        Assert.Equal(destination.EvaluationId, actual.EvaluationId);
        Assert.Equal(expectedNormality, actual.Determination);
        Assert.Equal(expectedNormality, actual.Result.AbnormalIndicator);
        Assert.Equal(description, actual.Result.Description);
    }

    [Fact]
    public void Map_From_EgfrLabResult_To_QuestLabResult_Tests()
    {
        var source = new EgfrLabResult
        {
            CenseoId = "X123",
            AccessionedDate = _applicationTime.UtcNow().AddDays(-1),
            CollectionDate = _applicationTime.UtcNow().AddMinutes(-50),
            CreatinineResult = (decimal)0.7,
            eGFRResult = 4,
            VendorLabTestId = 123,
            VendorLabTestNumber = "12345",
            MailDate = _applicationTime.UtcNow().AddDays(-2),
            ReceivedByEgfrDateTime = _applicationTime.UtcNow()
        };

        var mapper = CreateMapper();
        var actual = mapper.Map<QuestLabResult>(source);

        Assert.Equal(source.ReceivedByEgfrDateTime, actual.CreatedDateTime);
        Assert.Equal(source.CenseoId, actual.CenseoId);
        Assert.Equal(source.AccessionedDate, actual.AccessionedDate);
        Assert.Equal(source.CollectionDate, actual.CollectionDate);
        Assert.Equal(source.CreatinineResult, actual.CreatinineResult);
        Assert.Equal(source.eGFRResult, actual.eGFRResult);
        Assert.Equal(source.VendorLabTestId, actual.VendorLabTestId);
        Assert.Equal(source.VendorLabTestNumber, actual.VendorLabTestNumber);
        Assert.Equal(source.MailDate, actual.MailDate);
    }
    
    [Fact]
    public void Map_From_KedEgfrLabResult_To_LabResult_Tests()
    {
        var source = new KedEgfrLabResult
        {
            EvaluationId = 123,
            EgfrResult = 60.45m,
            EstimatedGlomerularFiltrationRateResultDescription = "test description",
            DateLabReceived = _applicationTime.UtcNow().AddMinutes(-50),
            ReceivedByEgfrDateTime = _applicationTime.UtcNow()
        };

        var mapper = CreateMapper();
        var actual = mapper.Map<LabResult>(source);

        Assert.Equal(source.ReceivedByEgfrDateTime, actual.CreatedDateTime);
        Assert.Equal(source.EgfrResult, actual.EgfrResult);
        Assert.Equal(source.DateLabReceived, actual.ReceivedDate);
        Assert.Equal(source.EstimatedGlomerularFiltrationRateResultDescription, actual.ResultDescription);
    }
    
    [Fact]
    public void Should_Map_NotPerformedReason_To_EgfrNotPerformed()
    {
        var mapper = CreateMapper();
        var result = mapper.Map<ExamNotPerformed>(NotPerformedReason.MemberRecentlyCompleted);
        Assert.Equal(1, result.NotPerformedReasonId);
        result = mapper.Map<ExamNotPerformed>(NotPerformedReason.ScheduledToComplete);
        Assert.Equal(2, result.NotPerformedReasonId);
        result = mapper.Map<ExamNotPerformed>(NotPerformedReason.MemberApprehension);
        Assert.Equal(3, result.NotPerformedReasonId);
        result = mapper.Map<ExamNotPerformed>(NotPerformedReason.NotInterested);
        Assert.Equal(4, result.NotPerformedReasonId);
        result = mapper.Map<ExamNotPerformed>(NotPerformedReason.TechnicalIssue);
        Assert.Equal(5, result.NotPerformedReasonId);
        result = mapper.Map<ExamNotPerformed>(NotPerformedReason.EnvironmentalIssue);
        Assert.Equal(6, result.NotPerformedReasonId);
        result = mapper.Map<ExamNotPerformed>(NotPerformedReason.NoSuppliesOrEquipment);
        Assert.Equal(7, result.NotPerformedReasonId);
        result = mapper.Map<ExamNotPerformed>(NotPerformedReason.InsufficientTraining);
        Assert.Equal(8, result.NotPerformedReasonId);
        result = mapper.Map<ExamNotPerformed>(NotPerformedReason.ClinicallyNotRelevant);
        Assert.Equal(9, result.NotPerformedReasonId);
    }
}