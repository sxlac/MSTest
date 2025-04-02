using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Refit;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using Signify.uACR.Core.ApiClients.InternalLabResultApi;
using Signify.uACR.Core.ApiClients.InternalLabResultApi.Responses;
using Signify.uACR.Core.Constants;
using Signify.uACR.Core.Data;
using Signify.uACR.Core.EventHandlers.Nsb;
using Signify.uACR.Core.Events;
using Signify.uACR.Core.Exceptions;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Signify.uACR.Core.Tests.EventHandlers.Nsb;

public sealed class InternalLabResultReceivedHandlerTests
{
    private readonly FakeApplicationTime _applicationTime = new();
    private readonly TestableMessageHandlerContext _fakeContext = new();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();
    private readonly IInternalLabResultApi _internalLabResultApi = A.Fake<IInternalLabResultApi>();
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly ITransactionSupplier _transactionSupplier = A.Fake<ITransactionSupplier>();

    private const string VendorDataJson =
        "{\"resourceType\":\"Bundle\",\"type\":\"searchset\",\"timestamp\":\"2024-11-28T15:58:12.006+00:00\",\"meta\":{\"lastUpdated\":\"2024-11-28T15:58:12.006+00:00\",\"versionId\":\"c2e962b0-6a07-488e-844e-a2b35666debc\"},\"entry\":[{\"fullUrl\":\"https://b2b-fhir-api.letsgetchecked-stg2.com/DiagnosticReport/9a386dbb-7c19-4cb9-b772-5822fbb617d6\",\"search\":{\"mode\":\"match\"},\"resource\":{\"resourceType\":\"DiagnosticReport\",\"meta\":{\"versionId\":\"bad878ea-743d-492e-a0a9-e7bb28c698c1\",\"lastUpdated\":\"2024-11-22T15:20:21.215+00:00\"},\"contained\":[{\"resourceType\":\"Patient\",\"id\":\"25ce82a2-1453-4922-9316-1a041f31ed2c\",\"identifier\":[{\"system\":\"http://ihr.signify.com/participantId\",\"value\":\"566215\"}],\"name\":[{\"family\":\"Doe\",\"given\":[\"John\"]}],\"telecom\":[{\"system\":\"phone\",\"value\":\"5029997778\"},{\"system\":\"email\",\"value\":\"bkutacho566215@letsgetchecked.com\"}],\"gender\":\"male\",\"birthDate\":\"1998-04-05\",\"address\":[{\"line\":[\"2485 Ice Town Rd\",\"Boston, Kentucky(KY)\"],\"city\":\"Boston\",\"state\":\"California\",\"postalCode\":\"40103\",\"country\":\"US\"}]},{\"resourceType\":\"Practitioner\",\"id\":\"8cc0a95d-8f2c-4594-8cca-f4c714da9fc0\",\"identifier\":[{\"system\":\"http://hl7.org/fhir/sid/us-npi\",\"value\":\"1821020579\"}],\"name\":[{\"family\":\"Mordkin\",\"given\":[\"Robert\"]}]},{\"resourceType\":\"Observation\",\"id\":\"b16b3d33-ecd8-4fcc-9496-b424118a70ea\",\"status\":\"final\",\"category\":[{\"coding\":[{\"system\":\"http://terminology.hl7.org/CodeSystem/observation-category\",\"code\":\"laboratory\",\"display\":\"Laboratory\"}],\"text\":\"Laboratory\"}],\"code\":{\"coding\":[{\"system\":\"http://loinc.org\",\"code\":\"2160-0\",\"display\":\"Creatinine\"}],\"text\":\"Creatinine\"},\"subject\":{\"reference\":\"#25ce82a2-1453-4922-9316-1a041f31ed2c\",\"display\":\"John Doe\"},\"effectiveDateTime\":\"2024-11-22T15:18:34.72+00:00\",\"performer\":[{\"reference\":\"#8cc0a95d-8f2c-4594-8cca-f4c714da9fc0\",\"display\":\"Robert Mordkin\"}],\"valueQuantity\":{\"value\":1,\"unit\":\"mg/dL\",\"system\":\"http://unitsofmeasure.org\",\"code\":\"mg/dL\"},\"note\":[{\"text\":\"Your test result shows your creatinine levels are normal. Creatinine is a waste product produced by your muscles. Your kidneys filter and remove creatinine from the blood and release it into your urine. The amount of creatinine produced depends on your size and muscle mass. Even though this test result is normal, if you are experiencing any symptoms, you should follow up with your healthcare provider. You can download your lab report to view your results and reference ranges.\"}],\"referenceRange\":[{\"low\":{\"value\":0.67,\"unit\":\"mg/dL\",\"system\":\"http://unitsofmeasure.org\",\"code\":\"mg/dL\"},\"high\":{\"value\":1.17,\"unit\":\"mg/dL\",\"system\":\"http://unitsofmeasure.org\",\"code\":\"mg/dL\"}}]},{\"resourceType\":\"Observation\",\"id\":\"6ce30801-319d-443d-b1fc-cfa8e019adbd\",\"status\":\"final\",\"category\":[{\"coding\":[{\"system\":\"http://terminology.hl7.org/CodeSystem/observation-category\",\"code\":\"laboratory\",\"display\":\"Laboratory\"}],\"text\":\"Laboratory\"}],\"code\":{\"coding\":[{\"system\":\"http://loinc.org\",\"code\":\"14959-1\",\"display\":\"Creatinine in Urine\"}],\"text\":\"Creatinine in Urine\"},\"subject\":{\"reference\":\"#25ce82a2-1453-4922-9316-1a041f31ed2c\",\"display\":\"John Doe\"},\"effectiveDateTime\":\"2024-11-22T15:18:34.72+00:00\",\"performer\":[{\"reference\":\"#8cc0a95d-8f2c-4594-8cca-f4c714da9fc0\",\"display\":\"Robert Mordkin\"}],\"valueQuantity\":{\"value\":120,\"unit\":\"mg/g\",\"system\":\"http://unitsofmeasure.org\",\"code\":\"mg/g\"},\"note\":[{\"text\":\"Your Urine Albumin Creatinine ratio (UACR) is normal. This test measures how much albumin is in your urine and found your levels to be normal. When high levels of albumin are found in the urine it can be an early indicator of kidney disease, however, your levels were normal. Please share this test result with your healthcare provider. Even though your test is normal, if you are experiencing any symptoms or are concerned about your health, you should speak to your healthcare provider.\"}],\"referenceRange\":[{\"low\":{\"value\":30,\"unit\":\"mg/g\",\"system\":\"http://unitsofmeasure.org\",\"code\":\"mg/g\"},\"high\":{\"value\":300,\"unit\":\"mg/g\",\"system\":\"http://unitsofmeasure.org\",\"code\":\"mg/g\"}}]},{\"resourceType\":\"Observation\",\"id\":\"4995f64e-6125-4fcd-9536-a8dc928b41ce\",\"status\":\"final\",\"category\":[{\"coding\":[{\"system\":\"http://terminology.hl7.org/CodeSystem/observation-category\",\"code\":\"laboratory\",\"display\":\"Laboratory\"}],\"text\":\"Laboratory\"}],\"code\":{\"coding\":[{\"system\":\"http://loinc.org\",\"code\":\"98979-8\",\"display\":\"Estimated Glomerular Filtration Rate\"}],\"text\":\"Estimated Glomerular Filtration Rate\"},\"subject\":{\"reference\":\"#25ce82a2-1453-4922-9316-1a041f31ed2c\",\"display\":\"John Doe\"},\"effectiveDateTime\":\"2024-11-22T15:18:34.72+00:00\",\"performer\":[{\"reference\":\"#8cc0a95d-8f2c-4594-8cca-f4c714da9fc0\",\"display\":\"Robert Mordkin\"}],\"valueQuantity\":{\"value\":90,\"unit\":\"ml/min/1.73M2\",\"system\":\"http://unitsofmeasure.org\",\"code\":\"ml/min/1.73m2\"},\"note\":[{\"text\":\"Your eGFR result is normal. This result is an estimated calculation that helps determine how well your kidneys are filtering blood using different factors including your creatinine level, age, and sex. A result between 60 and 90 shows slightly reduced kidney function. This can be a result of many factors including infection, certain medications, or health conditions or it could be an early sign of kidney disease. You should share this result with your healthcare provider to assess your risk of kidney disease. A result above 90 is optimal. The best ways to keep your kidneys healthy are staying hydrated, a healthy balanced diet, exercise, and not smoking. It’s important to check for health conditions such as diabetes, high blood pressure, and high cholesterol. If you have any of these conditions it is very important to keep your blood sugar levels, blood pressure, and cholesterol under control and to speak to your healthcare provider about how best to manage your risk.\"}],\"referenceRange\":[{\"low\":{\"value\":60,\"unit\":\"ml/min/1.73M2\",\"system\":\"http://unitsofmeasure.org\",\"code\":\"ml/min/1.73m2\"}}]}],\"identifier\":[{\"system\":\"http://letsgetchecked.com/results/barcode\",\"value\":\"LGC-0247-0920-8227\"},{\"system\":\"http://letsgetchecked.com/results/alphaCode\",\"value\":\"AQUBNF\"}],\"status\":\"final\",\"code\":{\"coding\":[{\"system\":\"http://loinc.org\",\"code\":\"98979-8\",\"display\":\"Estimated Glomerular Filtration Rate\"},{\"system\":\"http://loinc.org\",\"code\":\"2160-0\",\"display\":\"Creatinine\"},{\"system\":\"http://loinc.org\",\"code\":\"14959-1\",\"display\":\"Creatinine in Urine\"}]},\"subject\":{\"reference\":\"#25ce82a2-1453-4922-9316-1a041f31ed2c\",\"display\":\"John Doe\"},\"effectiveDateTime\":\"2024-11-22T15:18:34.72+00:00\",\"performer\":[{\"reference\":\"#8cc0a95d-8f2c-4594-8cca-f4c714da9fc0\",\"display\":\"Robert Mordkin\"}],\"result\":[{\"reference\":\"#b16b3d33-ecd8-4fcc-9496-b424118a70ea\"},{\"reference\":\"#6ce30801-319d-443d-b1fc-cfa8e019adbd\"},{\"reference\":\"#4995f64e-6125-4fcd-9536-a8dc928b41ce\"}],\"id\":\"9a386dbb-7c19-4cb9-b772-5822fbb617d6\"}}],\"total\":1,\"link\":[{\"relation\":\"self\",\"url\":\"https://b2b-fhir-api.letsgetchecked-stg2.com/DiagnosticReport?identifier=http%3a%2f%2fletsgetchecked.com%2fresults%2fbarcode%7cLGC-0247-0920-8227&_count=10&_skip=0\"}],\"id\":\"692e574d-853f-4178-89a1-7343d8efddbd\"}";

    private readonly JsonElement _vendorData = JsonDocument.Parse(VendorDataJson).RootElement;

    private InternalLabResultReceivedHandler CreateSubject()
        => new(A.Dummy<ILogger<InternalLabResultReceivedHandler>>(),
            _mediator,
            _transactionSupplier,
            _publishObservability,
            _applicationTime,
            _internalLabResultApi,
            _mapper);

    [Fact]
    public async Task Handle_LabResultReceivedEvent_HappyPath()
    {
        // Arrange
        var labResultReceivedEvent = new LabResultReceivedEvent
        {
            LabResultId = 12345678,
            VendorName = "LetsGetChecked",
            ProductCodes = new HashSet<string> {"EGFR", "UACR"},
            ReceivedDateTime = _applicationTime.UtcNow()
        };
        var getResultResponse = new GetResultResponse
        {
            LabResultId = 12345678,
            RequestId = Guid.NewGuid(),
            VendorName = "LetsGetChecked",
            TestNames = new HashSet<string> {"EGFR", "UACR"},
            ReceivedDateTime = _applicationTime.UtcNow(),
            VendorData = _vendorData
        };
        var kedUacrLabResult = new KedUacrLabResult
        {
            EvaluationId = 123,
            UacrResult = 60.45m,
            UrineAlbuminToCreatinineRatioResultDescription = "test description",
            DateLabReceived = _applicationTime.UtcNow().AddMinutes(-50),
            ReceivedByUacrDateTime = _applicationTime.UtcNow()
        };
        var fakeResponse = new ApiResponse<GetResultResponse>(
            new HttpResponseMessage(System.Net.HttpStatusCode.OK), getResultResponse, null!);
        A.CallTo(() => _internalLabResultApi.GetLabResultByLabResultId(A<string>._)).Returns(fakeResponse);
        A.CallTo(() => _mapper.Map<KedUacrLabResult>(A<JsonElement>._)).Returns(kedUacrLabResult);

        // Act
        await CreateSubject().Handle(labResultReceivedEvent, _fakeContext);

        // Assert
        A.CallTo(() => _mapper.Map<KedUacrLabResult>(A<JsonElement>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _internalLabResultApi.GetLabResultByLabResultId(A<string>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _publishObservability.RegisterEvent(
                A<ObservabilityEvent>.That.Matches(e =>
                    e.EventType == Observability.RmsIlrApi.GetLabResultByLabResultIdEvents), true))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _publishObservability.RegisterEvent(
                A<ObservabilityEvent>.That.Matches(e =>
                    e.EventType == Observability.RmsIlrApi.LabResultMappedEvents), true))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Handle_LabResultReceivedEvent_RaiseException()
    {
        // Arrange
        var labResultReceivedEvent = new LabResultReceivedEvent
        {
            LabResultId = 12345678,
            VendorName = "LetsGetChecked",
            ProductCodes = new HashSet<string> {"EGFR", "UACR"},
            ReceivedDateTime = _applicationTime.UtcNow()
        };
        var getResultResponse = new GetResultResponse
        {
            LabResultId = 12345678,
            RequestId = Guid.NewGuid(),
            VendorName = "LetsGetChecked",
            TestNames = new HashSet<string> {"EGFR", "UACR"},
            ReceivedDateTime = _applicationTime.UtcNow(),
            VendorData = _vendorData
        };
        var fakeResponse = new ApiResponse<GetResultResponse>(
            new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized), getResultResponse, null!);
        A.CallTo(() => _internalLabResultApi.GetLabResultByLabResultId(A<string>._)).Returns(fakeResponse);

        // Act
        await Assert.ThrowsAsync<GetResultResponseUnsuccessfulException>(() =>
            CreateSubject().Handle(labResultReceivedEvent, _fakeContext));

        // Assert
        A.CallTo(() => _internalLabResultApi.GetLabResultByLabResultId(A<string>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _publishObservability.RegisterEvent(
                A<ObservabilityEvent>.That.Matches(e =>
                    e.EventType == Observability.RmsIlrApi.GetLabResultByLabResultIdEvents), true))
            .MustHaveHappenedOnceExactly();
    }
}