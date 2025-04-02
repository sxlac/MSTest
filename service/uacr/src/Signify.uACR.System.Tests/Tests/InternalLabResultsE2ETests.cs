using System.Text.Json.Nodes;
using Dps.Labs.Webhook.Api.Test.Library.Models.Kafka;
using Dps.Labs.Webhook.Api.Test.Library.Models.Requests;
using Dps.Labs.Webhook.Api.Test.Library.Models.Responses;
using Signify.EvaluationsApi.Core.Values;
using Signify.uACR.System.Tests.Core.Models.NewRelic;
using ResultsReceived = Signify.uACR.Core.Events.Akka.ResultsReceived;

namespace Signify.uACR.System.Tests.Tests;

[TestClass, DoNotParallelize, TestCategory("regression")]
public class InternalLabResultsE2ETests : BaseTestActions
{
    public TestContext TestContext { get; set; }

    [ClassInitialize]
    public static async Task ClearWiremockMappings(TestContext testContext)
    {
        await Task.Delay(5000);
        await WiremockActions.ClearMappings("/LabsWebhookPm/");
    }

    [TestMethod]
    [DataRow(29.999d, 1.07f, "N", "Normal", "Green")]
    [DataRow(30.000d, 1.27f, "A", "Abnormal", "Red")]
    [DataRow(30.001d, 1.27f, "A", "Abnormal", "Red")]
    public async Task ANC_T1194_InternalLabResultsE2EHappyPathTest(double uacr, float creatinine, string nIndicator, string determination, string color)
    {
        var (_, _, evaluation) = await CoreApiActions.PrepareEvaluation();
        
        var resultObject = GetResults(uacr, creatinine);
        
        resultObject!["entry"]![0]["resource"]["contained"].AsArray().First(x => x["resourceType"]!.ToString().Equals("Patient"))["identifier"][0]["value"] = evaluation.EvaluationId.ToString();

        TestContext.WriteLine($"[{TestContext.TestName}]EvaluationID: " + evaluation.EvaluationId);
        
        var barCode = GetBarcode();
        var alphaCode = GetAlphaCode();
        var answersDict = GeneratePerformedAnswers(barCode, alphaCode);
        
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId, CoreApiActions.GetEvaluationAnswerList(answersDict));
        
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        
        // Setup Auth endpoint
        var authRequest = new JsonObject
        {
            ["method"] = "POST",
            ["urlPath"] = MockVendorAuthUrl
        };
        var authResponse = new JsonObject
        {
            ["status"] = 200,
            ["body"] = GetVendorAuthResponseBody()
        };
        await WiremockActions.SetupMapping("LabsWebhookPm-Auth", authRequest, authResponse);

        // Setup Report endpoint
        var reportRequest = new JsonObject
        {
            ["method"] = "GET",
            ["urlPath"] = MockReportUrl
        };
        
        var reportResponse = new JsonObject
        {
            ["status"] = 200,
            ["body"] = resultObject.ToJsonString()
        };

        await WiremockActions.SetupMapping("LabsWebhookPm-DiagnosticReport", reportRequest, reportResponse);
        
        // Send CreateResult api request to Labs-WebhookApi
        var result = new CreateResultRequest
        {
            body = new ResultRequestBody
            {
                tenantID = "2005",
                callbackURL = "https://wiremock.uat.signifyhealth.com/wiremock/LabsWebhookPm/DiagnosticReport",
                timestamp = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss.sssZ")
            },
            type = "FhirDiagnosticReport"
        };
        
        var createResultResponse = await WebhookApiActions.SendCreateResultRequest<ResultResponse>(result, vendor:LgcVendorName, testName:ValidExamType);
        createResultResponse.RequestId.Should().NotBeNullOrEmpty();
        
        var labsWebhookInbound = await CoreKafkaActions.GetLabsWebhookResultEvent<LabWebhookResultEvent>(createResultResponse.RequestId);
        labsWebhookInbound.EventId.Should().Be(createResultResponse.RequestId);
        labsWebhookInbound.TestName.Should().Be(ValidExamType);
        labsWebhookInbound.Vendor.Should().Be(LgcVendorName);
        
        var uacrLabResults = await GetLabResultByEvaluationId(evaluation.EvaluationId);
        uacrLabResults.UacrResult.Should().Be((decimal)uacr);
        uacrLabResults.ResultDescription.Should().Be(null);
        uacrLabResults.Normality.Should().Be(determination);
        uacrLabResults.NormalityCode.Should().Be(nIndicator);
        uacrLabResults.ReceivedDate.Should().Be(DateTime.MinValue);
        uacrLabResults.ResultColor.Should().Be(null);
        
        var uacrResultEvent = await CoreKafkaActions.GetUacrResultsReceivedEvent<ResultsReceived>(evaluation.EvaluationId);
        uacrResultEvent.Result.UacrResult.Should().Be((decimal)uacr);
        uacrResultEvent.Result.AbnormalIndicator.Should().Be(nIndicator);
        uacrResultEvent.IsBillable.Should().BeTrue();
        uacrResultEvent.Determination.Should().Be(nIndicator);
        
    }
    
    [TestMethod]
    [DataRow("U", "Undetermined", "NotAvailable")]
    public async Task ANC_T1195_InternalLabResultsUndeterminedE2EHappyPathTest(string nIndicator, string determination, string description)
    {
        var (_, _, evaluation) = await CoreApiActions.PrepareEvaluation();
        
        var resultObject = GetResultsUndetermined(description);
        
        resultObject!["entry"]![0]["resource"]["contained"].AsArray().First(x => x["resourceType"]!.ToString().Equals("Patient"))["identifier"][0]["value"] = evaluation.EvaluationId.ToString();
        
        TestContext.WriteLine($"[{TestContext.TestName}]EvaluationID: " + evaluation.EvaluationId);
        
        var barCode = GetBarcode();
        var alphaCode = GetAlphaCode();
        var answersDict = GeneratePerformedAnswers(barCode, alphaCode);
        
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId, CoreApiActions.GetEvaluationAnswerList(answersDict));
        
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        
        // Setup Auth endpoint
        var authRequest = new JsonObject
        {
            ["method"] = "POST",
            ["urlPath"] = MockVendorAuthUrl
        };
        var authResponse = new JsonObject
        {
            ["status"] = 200,
            ["body"] = GetVendorAuthResponseBody()
        };
        await WiremockActions.SetupMapping("LabsWebhookPm-Auth", authRequest, authResponse);

        // Setup Report endpoint
        var reportRequest = new JsonObject
        {
            ["method"] = "GET",
            ["urlPath"] = MockReportUrl
        };
        
        var reportResponse = new JsonObject
        {
            ["status"] = 200,
            ["body"] = resultObject.ToJsonString()
        };

        await WiremockActions.SetupMapping("LabsWebhookPm-DiagnosticReport", reportRequest, reportResponse);
        
        // Send CreateResult api request to Labs-WebhookApi
        var result = new CreateResultRequest
        {
            body = new ResultRequestBody
            {
                tenantID = "2005",
                callbackURL = "https://wiremock.uat.signifyhealth.com/wiremock/LabsWebhookPm/DiagnosticReport",
                timestamp = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss.sssZ")
            },
            type = "FhirDiagnosticReport"
        };
        
        var createResultResponse = await WebhookApiActions.SendCreateResultRequest<ResultResponse>(result, vendor:LgcVendorName, testName:ValidExamType);
        createResultResponse.RequestId.Should().NotBeNullOrEmpty();
        
        var labsWebhookInbound = await CoreKafkaActions.GetLabsWebhookResultEvent<LabWebhookResultEvent>(createResultResponse.RequestId);
        labsWebhookInbound.EventId.Should().Be(createResultResponse.RequestId);
        labsWebhookInbound.TestName.Should().Be(ValidExamType);
        labsWebhookInbound.Vendor.Should().Be(LgcVendorName);
        
        var uacrLabResults = await GetLabResultByEvaluationId(evaluation.EvaluationId);
        uacrLabResults.UacrResult.Should().Be(null);
        uacrLabResults.ResultDescription.Should().Be(description);
        uacrLabResults.Normality.Should().Be(determination);
        uacrLabResults.NormalityCode.Should().Be(nIndicator);
        uacrLabResults.ReceivedDate.Should().Be(DateTime.MinValue);
        uacrLabResults.ResultColor.Should().Be(null);
        
        var uacrResultEvent = await CoreKafkaActions.GetUacrResultsReceivedEvent<ResultsReceived>(evaluation.EvaluationId);
        uacrResultEvent.Result.UacrResult.Should().Be(null);
        uacrResultEvent.Result.AbnormalIndicator.Should().Be(nIndicator);
        uacrResultEvent.IsBillable.Should().BeFalse();
        uacrResultEvent.Determination.Should().Be(nIndicator);
        
    }

    [TestMethod]
    public async Task ANC_T1196_InternalLabResultsE2ENullEvalTest()
    {
        var resultObject = GetResults(29, 1.07f);

        resultObject!["entry"]![0]["resource"]["contained"].AsArray()
                .First(x => x["resourceType"]!.ToString().Equals("Patient"))["identifier"][0]["value"] = null;
        
        TestContext.WriteLine($"[{TestContext.TestName}] Result with No EvaluationId");

        // Setup Auth endpoint
        var authRequest = new JsonObject
        {
            ["method"] = "POST",
            ["urlPath"] = MockVendorAuthUrl
        };
        var authResponse = new JsonObject
        {
            ["status"] = 200,
            ["body"] = GetVendorAuthResponseBody()
        };
        await WiremockActions.SetupMapping("LabsWebhookPm-Auth", authRequest, authResponse);

        // Setup Report endpoint
        var reportRequest = new JsonObject
        {
            ["method"] = "GET",
            ["urlPath"] = MockReportUrl
        };

        var reportResponse = new JsonObject
        {
            ["status"] = 200,
            ["body"] = resultObject.ToJsonString()
        };

        await WiremockActions.SetupMapping("LabsWebhookPm-DiagnosticReport", reportRequest, reportResponse);

        // Send CreateResult api request to Labs-WebhookApi
        var result = new CreateResultRequest
        {
            body = new ResultRequestBody
            {
                tenantID = "2005",
                callbackURL = "https://wiremock.uat.signifyhealth.com/wiremock/LabsWebhookPm/DiagnosticReport",
                timestamp = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss.sssZ")
            },
            type = "FhirDiagnosticReport"
        };

        var createResultResponse =
            await WebhookApiActions.SendCreateResultRequest<ResultResponse>(result, vendor: LgcVendorName,
                testName: ValidExamType);
        createResultResponse.RequestId.Should().NotBeNullOrEmpty();

        var labsWebhookInbound =
            await CoreKafkaActions.GetLabsWebhookResultEvent<LabWebhookResultEvent>(createResultResponse.RequestId);
        labsWebhookInbound.EventId.Should().Be(createResultResponse.RequestId);
        labsWebhookInbound.TestName.Should().Be(ValidExamType);
        labsWebhookInbound.Vendor.Should().Be(LgcVendorName);

        var internalLabResult = await GetInternalLabResultByRequestId(createResultResponse.RequestId);
        
        var nrEventsAfter = await NewRelicActions.GetCustomEvent<FhirParseExceptionEvent>("TransactionError",
            new Dictionary<string, string> { { "error.class", "FhirParseException" }, {"error.message", $"FhirParsePatientException%LabResultId: {internalLabResult.LabResultId}"} });
        nrEventsAfter.Count.Should().BeGreaterThan(0);
    }
    
    [TestMethod]
    [Ignore("Ignored because test does not progress in pipeline")]
    public async Task ANC_T1197_InternalLabResultsE2ENullPatientResourceTest()
    {
        var resultObject = GetResults(29, 1.07f);

        resultObject!["entry"]![0]["resource"]["contained"].AsArray().Remove(resultObject!["entry"]![0]["resource"]["contained"].AsArray().First(x => x["resourceType"]!.ToString().Equals("Patient")));
        
        TestContext.WriteLine($"[{TestContext.TestName}] Result with No EvaluationId");

        // Setup Auth endpoint
        var authRequest = new JsonObject
        {
            ["method"] = "POST",
            ["urlPath"] = MockVendorAuthUrl
        };
        var authResponse = new JsonObject
        {
            ["status"] = 200,
            ["body"] = GetVendorAuthResponseBody()
        };
        await WiremockActions.SetupMapping("LabsWebhookPm-Auth", authRequest, authResponse);

        // Setup Report endpoint
        var reportRequest = new JsonObject
        {
            ["method"] = "GET",
            ["urlPath"] = MockReportUrl
        };

        var reportResponse = new JsonObject
        {
            ["status"] = 200,
            ["body"] = resultObject.ToJsonString()
        };

        await WiremockActions.SetupMapping("LabsWebhookPm-DiagnosticReport", reportRequest, reportResponse);

        // Send CreateResult api request to Labs-WebhookApi
        var result = new CreateResultRequest
        {
            body = new ResultRequestBody
            {
                tenantID = "2005",
                callbackURL = "https://wiremock.uat.signifyhealth.com/wiremock/LabsWebhookPm/DiagnosticReport",
                timestamp = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss.sssZ")
            },
            type = "FhirDiagnosticReport"
        };

        var createResultResponse =
            await WebhookApiActions.SendCreateResultRequest<ResultResponse>(result, vendor: LgcVendorName,
                testName: ValidExamType);
        createResultResponse.RequestId.Should().NotBeNullOrEmpty();

        var labsWebhookInbound =
            await CoreKafkaActions.GetLabsWebhookResultEvent<LabWebhookResultEvent>(createResultResponse.RequestId);
        labsWebhookInbound.EventId.Should().Be(createResultResponse.RequestId);
        labsWebhookInbound.TestName.Should().Be(ValidExamType);
        labsWebhookInbound.Vendor.Should().Be(LgcVendorName);

        var internalLabResult = await GetInternalLabResultByRequestId(createResultResponse.RequestId);
        
        var nrEventsAfter = await NewRelicActions.GetCustomEvent<FhirParseExceptionEvent>("TransactionError",
            new Dictionary<string, string> { { "error.class", "FhirParseException" }, {"error.message", $"FhirParsePatientException%LabResultId: {internalLabResult.LabResultId}"} });
        nrEventsAfter.Count.Should().BeGreaterThan(0);
    }

    [TestMethod]
    [Ignore("Ignored because test does not progress in pipeline")]
    public async Task ANC_T1198_InternalLabResultsE2ENullResultResourceTest()
    {
        var (_, _, evaluation) = await CoreApiActions.PrepareEvaluation();

        var resultObject = GetResults(29, 1.07f);
        
        resultObject!["entry"]![0]["resource"]["contained"].AsArray().First(x => x["resourceType"]!.ToString().Equals("Patient"))["identifier"][0]["value"] = evaluation.EvaluationId.ToString();
        resultObject!["entry"]![0]["resource"]["contained"].AsArray()
            .Remove(resultObject!["entry"]![0]["resource"]["contained"].AsArray()
                    .Where(x=>x["resourceType"]!.ToString().Equals("Observation"))
                    .First(x=>x!["code"]["text"].ToString().Equals("Creatinine in Urine")));

        TestContext.WriteLine($"[{TestContext.TestName}] Result with No Result");

        // Setup Auth endpoint
        var authRequest = new JsonObject
        {
            ["method"] = "POST",
            ["urlPath"] = MockVendorAuthUrl
        };
        var authResponse = new JsonObject
        {
            ["status"] = 200,
            ["body"] = GetVendorAuthResponseBody()
        };
        await WiremockActions.SetupMapping("LabsWebhookPm-Auth", authRequest, authResponse);

        // Setup Report endpoint
        var reportRequest = new JsonObject
        {
            ["method"] = "GET",
            ["urlPath"] = MockReportUrl
        };

        var reportResponse = new JsonObject
        {
            ["status"] = 200,
            ["body"] = resultObject.ToJsonString()
        };

        await WiremockActions.SetupMapping("LabsWebhookPm-DiagnosticReport", reportRequest, reportResponse);

        // Send CreateResult api request to Labs-WebhookApi
        var result = new CreateResultRequest
        {
            body = new ResultRequestBody
            {
                tenantID = "2005",
                callbackURL = "https://wiremock.uat.signifyhealth.com/wiremock/LabsWebhookPm/DiagnosticReport",
                timestamp = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss.sssZ")
            },
            type = "FhirDiagnosticReport"
        };

        var createResultResponse =
            await WebhookApiActions.SendCreateResultRequest<ResultResponse>(result, vendor: LgcVendorName,
                testName: ValidExamType);
        createResultResponse.RequestId.Should().NotBeNullOrEmpty();

        var labsWebhookInbound =
            await CoreKafkaActions.GetLabsWebhookResultEvent<LabWebhookResultEvent>(createResultResponse.RequestId);
        labsWebhookInbound.EventId.Should().Be(createResultResponse.RequestId);
        labsWebhookInbound.TestName.Should().Be(ValidExamType);
        labsWebhookInbound.Vendor.Should().Be(LgcVendorName);

        var internalLabResult = await GetInternalLabResultByRequestId(createResultResponse.RequestId);
        
        var nrEventsAfter = await NewRelicActions.GetCustomEvent<FhirParseExceptionEvent>("TransactionError",
            new Dictionary<string, string> { { "error.class", "FhirParseException" }, {"error.message", $"FhirParseObservationException%LabResultId: {internalLabResult.LabResultId}"} });
        nrEventsAfter.Count.Should().BeGreaterThan(0);
    }
    
    [TestMethod]
    [Ignore("Ignored because test does not progress in pipeline")]
    public async Task ANC_T1199_InternalLabResultsE2ENullResultDescriptionTest()
    {
        var (_, _, evaluation) = await CoreApiActions.PrepareEvaluation();

        var resultObject = GetResults(29, 1.07f);

        resultObject!["entry"]![0]["resource"]["contained"].AsArray().First(x => x["resourceType"]!.ToString().Equals("Patient"))["identifier"][0]["value"] = evaluation.EvaluationId.ToString();
        resultObject!["entry"]![0]["resource"]["contained"].AsArray()
                .Where(x=>x["resourceType"]!.ToString().Equals("Observation")).First(x=>x["code"]!["text"].ToString().Equals("Creatinine in Urine"))["note"][0]["text"]=null;

        TestContext.WriteLine($"[{TestContext.TestName}] Result with No Result");

        // Setup Auth endpoint
        var authRequest = new JsonObject
        {
            ["method"] = "POST",
            ["urlPath"] = MockVendorAuthUrl
        };
        var authResponse = new JsonObject
        {
            ["status"] = 200,
            ["body"] = GetVendorAuthResponseBody()
        };
        await WiremockActions.SetupMapping("LabsWebhookPm-Auth", authRequest, authResponse);

        // Setup Report endpoint
        var reportRequest = new JsonObject
        {
            ["method"] = "GET",
            ["urlPath"] = MockReportUrl
        };

        var reportResponse = new JsonObject
        {
            ["status"] = 200,
            ["body"] = resultObject.ToJsonString()
        };

        await WiremockActions.SetupMapping("LabsWebhookPm-DiagnosticReport", reportRequest, reportResponse);

        // Send CreateResult api request to Labs-WebhookApi
        var result = new CreateResultRequest
        {
            body = new ResultRequestBody
            {
                tenantID = "2005",
                callbackURL = "https://wiremock.uat.signifyhealth.com/wiremock/LabsWebhookPm/DiagnosticReport",
                timestamp = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss.sssZ")
            },
            type = "FhirDiagnosticReport"
        };

        var createResultResponse =
            await WebhookApiActions.SendCreateResultRequest<ResultResponse>(result, vendor: LgcVendorName,
                testName: ValidExamType);
        createResultResponse.RequestId.Should().NotBeNullOrEmpty();

        var labsWebhookInbound =
            await CoreKafkaActions.GetLabsWebhookResultEvent<LabWebhookResultEvent>(createResultResponse.RequestId);
        labsWebhookInbound.EventId.Should().Be(createResultResponse.RequestId);
        labsWebhookInbound.TestName.Should().Be(ValidExamType);
        labsWebhookInbound.Vendor.Should().Be(LgcVendorName);

        var internalLabResult = await GetInternalLabResultByRequestId(createResultResponse.RequestId);
        
        var nrEventsAfter = await NewRelicActions.GetCustomEvent<FhirParseExceptionEvent>("TransactionError",
            new Dictionary<string, string> { { "error.class", "FhirParseException" }, {"error.message", $"FhirParseObservationException%LabResultId: {internalLabResult.LabResultId}"} });
        nrEventsAfter.Count.Should().BeGreaterThan(0);
    }
    
    [TestMethod]
    [Ignore("Ignored because test does not progress in pipeline")]
    public async Task ANC_T1200_InternalLabResultsE2EInvalidResultUnit()
    {
        var (_, _, evaluation) = await CoreApiActions.PrepareEvaluation();
        
        // Add results with invalid units
        var resultObject = GetResults(29, "mL/min/1.73m2", "mL/min/1.73m2", 1.07f, "mg/g", "mg/g");
        
        resultObject!["entry"]![0]["resource"]["contained"].AsArray().First(x => x["resourceType"]!.ToString().Equals("Patient"))["identifier"][0]["value"] = evaluation.EvaluationId.ToString();
       
        
        TestContext.WriteLine($"[{TestContext.TestName}]EvaluationID: " + evaluation.EvaluationId);
        
        var barCode = GetBarcode();
        var alphaCode = GetAlphaCode();
        var answersDict = GeneratePerformedAnswers(barCode, alphaCode);
        
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId, CoreApiActions.GetEvaluationAnswerList(answersDict));
        
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        
        // Setup Auth endpoint
        var authRequest = new JsonObject
        {
            ["method"] = "POST",
            ["urlPath"] = MockVendorAuthUrl
        };
        var authResponse = new JsonObject
        {
            ["status"] = 200,
            ["body"] = GetVendorAuthResponseBody()
        };
        await WiremockActions.SetupMapping("LabsWebhookPm-Auth", authRequest, authResponse);

        // Setup Report endpoint
        var reportRequest = new JsonObject
        {
            ["method"] = "GET",
            ["urlPath"] = MockReportUrl
        };
        
        var reportResponse = new JsonObject
        {
            ["status"] = 200,
            ["body"] = resultObject.ToJsonString()
        };

        await WiremockActions.SetupMapping("LabsWebhookPm-DiagnosticReport", reportRequest, reportResponse);
        
        // Send CreateResult api request to Labs-WebhookApi
        var result = new CreateResultRequest
        {
            body = new ResultRequestBody
            {
                tenantID = "2005",
                callbackURL = "https://wiremock.uat.signifyhealth.com/wiremock/LabsWebhookPm/DiagnosticReport",
                timestamp = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss.sssZ")
            },
            type = "FhirDiagnosticReport"
        };
        
        var createResultResponse = await WebhookApiActions.SendCreateResultRequest<ResultResponse>(result, vendor:LgcVendorName, testName:ValidExamType);
        createResultResponse.RequestId.Should().NotBeNullOrEmpty();
        
        var labsWebhookInbound = await CoreKafkaActions.GetLabsWebhookResultEvent<LabWebhookResultEvent>(createResultResponse.RequestId);
        labsWebhookInbound.EventId.Should().Be(createResultResponse.RequestId);
        labsWebhookInbound.TestName.Should().Be(ValidExamType);
        labsWebhookInbound.Vendor.Should().Be(LgcVendorName);
       
        
        var nrEventsAfter = await NewRelicActions.GetCustomEvent<FhirParseExceptionEvent>("TransactionError",
            new Dictionary<string, string> { { "error.class", "FhirParseException" }, {"error.message", $"FhirParseObservationException%EvaluationId: {evaluation.EvaluationId}"} });
        nrEventsAfter.Count.Should().BeGreaterThan(0);
    }

    private static JsonNode GetResults(double uacrResult, float createnine)
    {
        return GetResults(uacrResult, "mg/g", "mg/g", createnine, "mL/min/1.73m2", "mL/min/1.73m2");
    }
    
    private static JsonNode GetResults(double uacrResult, string uacrUnit, string uacrcode, float createnine, string createnineUnit, string createninecode)
    {
        const string resultFilePath = "../../../../Signify.uACR.System.Tests.Core/Data/sampleFhirResult.json";
        var resultObject = JsonNode.Parse(File.ReadAllText(resultFilePath));
        
        resultObject!["entry"]![0]["resource"]["contained"].AsArray().Where(x=>x["resourceType"]!.ToString().Equals("Observation")).First(x=>x!["code"]["text"].ToString().Equals("Creatinine"))["valueQuantity"]!["value"] = createnine.ToString();
        resultObject!["entry"]![0]["resource"]["contained"].AsArray().Where(x=>x["resourceType"]!.ToString().Equals("Observation")).First(x=>x!["code"]["text"].ToString().Equals("Creatinine"))["valueQuantity"]!["code"] = createninecode;
        resultObject!["entry"]![0]["resource"]["contained"].AsArray().Where(x=>x["resourceType"]!.ToString().Equals("Observation")).First(x=>x!["code"]["text"].ToString().Equals("Creatinine"))["valueQuantity"]!["unit"] = createnineUnit;
        resultObject!["entry"]![0]["resource"]["contained"].AsArray().Where(x=>x["resourceType"]!.ToString().Equals("Observation")).First(x=>x!["code"]["text"].ToString().Equals("Creatinine in Urine"))["valueQuantity"]["value"] = uacrResult.ToString();
        resultObject!["entry"]![0]["resource"]["contained"].AsArray().Where(x=>x["resourceType"]!.ToString().Equals("Observation")).First(x=>x!["code"]["text"].ToString().Equals("Creatinine in Urine"))["valueQuantity"]["code"] = uacrcode;
        resultObject!["entry"]![0]["resource"]["contained"].AsArray().Where(x=>x["resourceType"]!.ToString().Equals("Observation")).First(x=>x!["code"]["text"].ToString().Equals("Creatinine in Urine"))["valueQuantity"]["unit"] = uacrUnit;
        return resultObject;
    }
    
    private static JsonNode GetResultsUndetermined(string description)
    {
        const string resultFilePath = "../../../../Signify.uACR.System.Tests.Core/Data/sampleFhirUndeterminedResult.json";
        var resultObject = JsonNode.Parse(File.ReadAllText(resultFilePath));

        resultObject!["entry"]![0]["resource"]["contained"]
            .AsArray()
            .Where(x => x["resourceType"]!.ToString().Equals("Observation"))
            .First(x => x!["code"]["coding"]!.AsArray().Any(c => c["code"]!.ToString().Equals(Signify.uACR.Core.Constants.Fhir.CreatinineInUrineLoincCode)))["interpretation"]![0]!["text"] = description;
        return resultObject;
    }
    
    private static string GetVendorAuthResponseBody()
    {
        const string filePath = "../../../../Signify.uACR.System.Tests.Core/Data/vendorAuthResponseBody.json";
        var resultObject = JsonNode.Parse(File.ReadAllText(filePath));
        return resultObject!.ToJsonString();
    }
    
    
}