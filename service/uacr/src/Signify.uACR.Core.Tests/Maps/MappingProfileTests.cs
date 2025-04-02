using AutoMapper;
using UacrNsbEvents;
using Signify.uACR.Core.Data.Entities;
using Signify.uACR.Core.Events.Akka;
using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Signify.uACR.Core.ApiClients.MemberApi.Responses;
using Signify.uACR.Core.DI.Configs;
using Signify.uACR.Core.Events;
using Xunit;

namespace Signify.uACR.Core.Tests.Maps;

public class MappingProfileTests
{
    private static IMapper CreateMapper()
    {
        var services = new ServiceCollection().AddSingleton<MappingProfileTests>();
        return AutoMapperConfig.AddAutoMapper(services);
    }

    [Fact]
    public void Map_FromEvalReceived_To_Exam_MapsAllProperties()
    {
        var date = new DateTime(2020, 01, 01, 01, 01, 01, DateTimeKind.Utc);

        const long evaluationId = 1;
        const int providerId = 2;
        const long memberId = 3;
        const int memberPlanId = 4;
        var createdDateTime = new DateTimeOffset(date);
        var receivedDateTime = date.AddMinutes(1);
        var dateOfService = date.Date;
        var receivedByProcessManager = receivedDateTime.AddMinutes(1);

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
            ReceivedByUacrProcessManagerDateTime = receivedByProcessManager
        };

        var expectedExam = new Exam
        {
            ApplicationId = nameof(ApplicationId),
            EvaluationId = evaluationId,
            ProviderId = providerId,
            MemberId = memberId,
            MemberPlanId = memberPlanId,
            EvaluationCreatedDateTime = createdDateTime.DateTime,
            EvaluationReceivedDateTime = receivedDateTime,
            DateOfService = dateOfService,
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
    public void Map_FromMemberInfo_To_Exam_MapsAllProperties()
    {
        var memberInfo = new MemberInfo
        {
            MemberId = 1234567,
            CenseoId = "CenseoId",
            FirstName = "FirstName",
            MiddleName = "MiddleName",
            LastName = "LastName",
            DateOfBirth = DateTime.UtcNow,
            AddressLineOne = "AddressLineOne",
            AddressLineTwo = "AddressLineTwo",
            City = "City",
            State = "State",
            ZipCode = "ZipCode"
        };

        var expectedExam = new Exam
        {
            MemberId = memberInfo.MemberId,
            CenseoId = memberInfo.CenseoId,
            FirstName = memberInfo.FirstName,
            MiddleName = memberInfo.MiddleName,
            LastName = memberInfo.LastName,
            DateOfBirth = DateOnly.FromDateTime(memberInfo.DateOfBirth.Value),
            AddressLineOne = memberInfo.AddressLineOne,
            AddressLineTwo = memberInfo.AddressLineTwo,
            City = memberInfo.City,
            State = memberInfo.State,
            ZipCode = memberInfo.ZipCode
        };

        var mapper = CreateMapper();

        var actualExam = mapper.Map<Exam>(memberInfo);

        Assert.Equal(expectedExam.MemberId, actualExam.MemberId);
        Assert.Equal(expectedExam.CenseoId, actualExam.CenseoId);
        Assert.Equal(expectedExam.FirstName, actualExam.FirstName);
        Assert.Equal(expectedExam.MiddleName, actualExam.MiddleName);
        Assert.Equal(expectedExam.LastName, actualExam.LastName);
        Assert.Equal(expectedExam.DateOfBirth, actualExam.DateOfBirth);
        Assert.Equal(expectedExam.AddressLineOne, actualExam.AddressLineOne);
        Assert.Equal(expectedExam.AddressLineTwo, actualExam.AddressLineTwo);
        Assert.Equal(expectedExam.City, actualExam.City);
        Assert.Equal(expectedExam.State, actualExam.State);
        Assert.Equal(expectedExam.ZipCode, actualExam.ZipCode);
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

        Assert.Equal(ExamStatusCode.ExamPerformed.ExamStatusCodeId, actual.ExamStatusCodeId);
        Assert.Equal(source.StatusDateTime, actual.StatusDateTime);
        Assert.Equal(source.ExamId, actual.ExamId);

        Assert.Null(actual.Exam);
        Assert.Null(actual.ExamStatusCode);
    }
    
    [Fact]
    public void Map_FromString_ToKedUacrLabResult_Exists()
    {
        var json = "{\"resourceType\":\"Bundle\",\"type\":\"searchset\",\"timestamp\":\"2024-11-28T15:58:12.006+00:00\",\"meta\":{\"lastUpdated\":\"2024-11-28T15:58:12.006+00:00\",\"versionId\":\"c2e962b0-6a07-488e-844e-a2b35666debc\"},\"entry\":[{\"fullUrl\":\"https://b2b-fhir-api.letsgetchecked-stg2.com/DiagnosticReport/9a386dbb-7c19-4cb9-b772-5822fbb617d6\",\"search\":{\"mode\":\"match\"},\"resource\":{\"resourceType\":\"DiagnosticReport\",\"meta\":{\"versionId\":\"bad878ea-743d-492e-a0a9-e7bb28c698c1\",\"lastUpdated\":\"2024-11-22T15:20:21.215+00:00\"},\"contained\":[{\"resourceType\":\"Patient\",\"id\":\"25ce82a2-1453-4922-9316-1a041f31ed2c\",\"identifier\":[{\"system\":\"http://ihr.signify.com/participantId\",\"value\":\"566215\"}],\"name\":[{\"family\":\"Doe\",\"given\":[\"John\"]}],\"telecom\":[{\"system\":\"phone\",\"value\":\"5029997778\"},{\"system\":\"email\",\"value\":\"bkutacho566215@letsgetchecked.com\"}],\"gender\":\"male\",\"birthDate\":\"1998-04-05\",\"address\":[{\"line\":[\"2485 Ice Town Rd\",\"Boston, Kentucky(KY)\"],\"city\":\"Boston\",\"state\":\"California\",\"postalCode\":\"40103\",\"country\":\"US\"}]},{\"resourceType\":\"Practitioner\",\"id\":\"8cc0a95d-8f2c-4594-8cca-f4c714da9fc0\",\"identifier\":[{\"system\":\"http://hl7.org/fhir/sid/us-npi\",\"value\":\"1821020579\"}],\"name\":[{\"family\":\"Mordkin\",\"given\":[\"Robert\"]}]},{\"resourceType\":\"Observation\",\"id\":\"b16b3d33-ecd8-4fcc-9496-b424118a70ea\",\"status\":\"final\",\"category\":[{\"coding\":[{\"system\":\"http://terminology.hl7.org/CodeSystem/observation-category\",\"code\":\"laboratory\",\"display\":\"Laboratory\"}],\"text\":\"Laboratory\"}],\"code\":{\"coding\":[{\"system\":\"http://loinc.org\",\"code\":\"2160-0\",\"display\":\"Creatinine\"}],\"text\":\"Creatinine\"},\"subject\":{\"reference\":\"#25ce82a2-1453-4922-9316-1a041f31ed2c\",\"display\":\"John Doe\"},\"effectiveDateTime\":\"2024-11-22T15:18:34.72+00:00\",\"performer\":[{\"reference\":\"#8cc0a95d-8f2c-4594-8cca-f4c714da9fc0\",\"display\":\"Robert Mordkin\"}],\"valueQuantity\":{\"value\":1,\"unit\":\"mg/dL\",\"system\":\"http://unitsofmeasure.org\",\"code\":\"mg/dL\"},\"note\":[{\"text\":\"Your test result shows your creatinine levels are normal. Creatinine is a waste product produced by your muscles. Your kidneys filter and remove creatinine from the blood and release it into your urine. The amount of creatinine produced depends on your size and muscle mass. Even though this test result is normal, if you are experiencing any symptoms, you should follow up with your healthcare provider. You can download your lab report to view your results and reference ranges.\"}],\"referenceRange\":[{\"low\":{\"value\":0.67,\"unit\":\"mg/dL\",\"system\":\"http://unitsofmeasure.org\",\"code\":\"mg/dL\"},\"high\":{\"value\":1.17,\"unit\":\"mg/dL\",\"system\":\"http://unitsofmeasure.org\",\"code\":\"mg/dL\"}}]},{\"resourceType\":\"Observation\",\"id\":\"6ce30801-319d-443d-b1fc-cfa8e019adbd\",\"status\":\"final\",\"category\":[{\"coding\":[{\"system\":\"http://terminology.hl7.org/CodeSystem/observation-category\",\"code\":\"laboratory\",\"display\":\"Laboratory\"}],\"text\":\"Laboratory\"}],\"code\":{\"coding\":[{\"system\":\"http://loinc.org\",\"code\":\"14959-1\",\"display\":\"Creatinine in Urine\"}],\"text\":\"Creatinine in Urine\"},\"subject\":{\"reference\":\"#25ce82a2-1453-4922-9316-1a041f31ed2c\",\"display\":\"John Doe\"},\"effectiveDateTime\":\"2024-11-22T15:18:34.72+00:00\",\"performer\":[{\"reference\":\"#8cc0a95d-8f2c-4594-8cca-f4c714da9fc0\",\"display\":\"Robert Mordkin\"}],\"valueQuantity\":{\"value\":120,\"unit\":\"mg/g\",\"system\":\"http://unitsofmeasure.org\",\"code\":\"mg/g\"},\"note\":[{\"text\":\"Your Urine Albumin Creatinine ratio (UACR) is normal. This test measures how much albumin is in your urine and found your levels to be normal. When high levels of albumin are found in the urine it can be an early indicator of kidney disease, however, your levels were normal. Please share this test result with your healthcare provider. Even though your test is normal, if you are experiencing any symptoms or are concerned about your health, you should speak to your healthcare provider.\"}],\"referenceRange\":[{\"low\":{\"value\":30,\"unit\":\"mg/g\",\"system\":\"http://unitsofmeasure.org\",\"code\":\"mg/g\"},\"high\":{\"value\":300,\"unit\":\"mg/g\",\"system\":\"http://unitsofmeasure.org\",\"code\":\"mg/g\"}}]},{\"resourceType\":\"Observation\",\"id\":\"4995f64e-6125-4fcd-9536-a8dc928b41ce\",\"status\":\"final\",\"category\":[{\"coding\":[{\"system\":\"http://terminology.hl7.org/CodeSystem/observation-category\",\"code\":\"laboratory\",\"display\":\"Laboratory\"}],\"text\":\"Laboratory\"}],\"code\":{\"coding\":[{\"system\":\"http://loinc.org\",\"code\":\"98979-8\",\"display\":\"Estimated Glomerular Filtration Rate\"}],\"text\":\"Estimated Glomerular Filtration Rate\"},\"subject\":{\"reference\":\"#25ce82a2-1453-4922-9316-1a041f31ed2c\",\"display\":\"John Doe\"},\"effectiveDateTime\":\"2024-11-22T15:18:34.72+00:00\",\"performer\":[{\"reference\":\"#8cc0a95d-8f2c-4594-8cca-f4c714da9fc0\",\"display\":\"Robert Mordkin\"}],\"valueQuantity\":{\"value\":90,\"unit\":\"ml/min/1.73M2\",\"system\":\"http://unitsofmeasure.org\",\"code\":\"ml/min/1.73m2\"},\"note\":[{\"text\":\"Your eGFR result is normal. This result is an estimated calculation that helps determine how well your kidneys are filtering blood using different factors including your creatinine level, age, and sex. A result between 60 and 90 shows slightly reduced kidney function. This can be a result of many factors including infection, certain medications, or health conditions or it could be an early sign of kidney disease. You should share this result with your healthcare provider to assess your risk of kidney disease. A result above 90 is optimal. The best ways to keep your kidneys healthy are staying hydrated, a healthy balanced diet, exercise, and not smoking. It’s important to check for health conditions such as diabetes, high blood pressure, and high cholesterol. If you have any of these conditions it is very important to keep your blood sugar levels, blood pressure, and cholesterol under control and to speak to your healthcare provider about how best to manage your risk.\"}],\"referenceRange\":[{\"low\":{\"value\":60,\"unit\":\"ml/min/1.73M2\",\"system\":\"http://unitsofmeasure.org\",\"code\":\"ml/min/1.73m2\"}}]}],\"identifier\":[{\"system\":\"http://letsgetchecked.com/results/barcode\",\"value\":\"LGC-0247-0920-8227\"},{\"system\":\"http://letsgetchecked.com/results/alphaCode\",\"value\":\"AQUBNF\"}],\"status\":\"final\",\"code\":{\"coding\":[{\"system\":\"http://loinc.org\",\"code\":\"98979-8\",\"display\":\"Estimated Glomerular Filtration Rate\"},{\"system\":\"http://loinc.org\",\"code\":\"2160-0\",\"display\":\"Creatinine\"},{\"system\":\"http://loinc.org\",\"code\":\"14959-1\",\"display\":\"Creatinine in Urine\"}]},\"subject\":{\"reference\":\"#25ce82a2-1453-4922-9316-1a041f31ed2c\",\"display\":\"John Doe\"},\"effectiveDateTime\":\"2024-11-22T15:18:34.72+00:00\",\"performer\":[{\"reference\":\"#8cc0a95d-8f2c-4594-8cca-f4c714da9fc0\",\"display\":\"Robert Mordkin\"}],\"result\":[{\"reference\":\"#b16b3d33-ecd8-4fcc-9496-b424118a70ea\"},{\"reference\":\"#6ce30801-319d-443d-b1fc-cfa8e019adbd\"},{\"reference\":\"#4995f64e-6125-4fcd-9536-a8dc928b41ce\"}],\"id\":\"9a386dbb-7c19-4cb9-b772-5822fbb617d6\"}}],\"total\":1,\"link\":[{\"relation\":\"self\",\"url\":\"https://b2b-fhir-api.letsgetchecked-stg2.com/DiagnosticReport?identifier=http%3a%2f%2fletsgetchecked.com%2fresults%2fbarcode%7cLGC-0247-0920-8227&_count=10&_skip=0\"}],\"id\":\"692e574d-853f-4178-89a1-7343d8efddbd\"}";
        var source = JsonDocument.Parse(json).RootElement;
        
        var mapper = CreateMapper();
        var actual = mapper.Map<KedUacrLabResult>(source);

        Assert.NotNull(actual);
    }
}