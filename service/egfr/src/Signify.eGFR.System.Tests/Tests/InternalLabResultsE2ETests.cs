using System.Text.Json.Nodes;
using Dps.Labs.Webhook.Api.Test.Library.Models.Kafka;
using Dps.Labs.Webhook.Api.Test.Library.Models.Requests;
using Dps.Labs.Webhook.Api.Test.Library.Models.Responses;
using Signify.eGFR.System.Tests.Core.Actions;
using Signify.eGFR.System.Tests.Core.Models.NewRelic;
using Signify.EvaluationsApi.Core.Values;
using ResultsReceived = Signify.eGFR.Core.Events.Akka.ResultsReceived;

namespace Signify.eGFR.System.Tests.Tests;

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
    [DataRow(59.999, 3, "A")]
    [DataRow(60.001, 2, "N")]
    public async Task ANC_T1201_InternalLabResultsE2EHappyPathTest(double egfr, int normalityIndicator, string normality)
    {
        var (_, _, evaluation) = await CoreApiActions.PrepareEvaluation();
        
        var resultObject = GetResults(egfr);
        
        resultObject!["entry"]![0]["resource"]["contained"].AsArray().First(x => x["resourceType"]!.ToString().Equals("Patient"))["identifier"][0]["value"] = evaluation.EvaluationId.ToString();
        var description =
            resultObject!["entry"]![0]["resource"]["contained"].AsArray()
                .Where(x=>x["resourceType"]!.ToString().Equals("Observation")).First(x=>x["code"]!["text"].ToString().Equals("Estimated Glomerular Filtration Rate"))["note"][0]["text"].ToString();
        
        TestContext.WriteLine($"[{TestContext.TestName}]EvaluationID: " + evaluation.EvaluationId);
        
        var answersDict = GenerateKedPerformedAnswers();
        
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
        
        var egfrExam = await GetExamByEvaluationId(evaluation.EvaluationId);
        var examId = egfrExam.ExamId;
        
        var labsWebhookInbound = await CoreKafkaActions.GetLabsWebhookResultEvent<LabWebhookResultEvent>(createResultResponse.RequestId);
        labsWebhookInbound.EventId.Should().Be(createResultResponse.RequestId);
        labsWebhookInbound.TestName.Should().Be(ValidExamType);
        labsWebhookInbound.Vendor.Should().Be(LgcVendorName);
        
        var egfrLabResults = await GetLabResultByExamId(examId);
        egfrLabResults.EgfrResult.Should().Be((decimal)egfr);
        egfrLabResults.ResultDescription.Should().Be(null);
        egfrLabResults.NormalityIndicatorId.Should().Be(normalityIndicator);
        egfrLabResults.ReceivedDate.Should().Be(DateTime.MinValue);
        
        var egfrResultEvent = await CoreKafkaActions.GetEgfrResultsReceivedEvent<ResultsReceived>(evaluation.EvaluationId);
        egfrResultEvent.Result.Result.Should().Be((decimal)egfr);
        egfrResultEvent.Result.AbnormalIndicator.Should().Be(normality);
        egfrResultEvent.IsBillable.Should().BeTrue();
        egfrResultEvent.Determination.Should().Be(normality);
        
    }
    
    [TestMethod]
    [DataRow(1, "U", "NotAvailable")]
    public async Task ANC_T1202_InternalLabResultsUndeterminedE2EHappyPathTest(int normalityIndicator, string normality, string description)
    {
        var (_, _, evaluation) = await CoreApiActions.PrepareEvaluation();
        
        var resultObject = GetResultsUndetermined(description);
        
        resultObject!["entry"]![0]["resource"]["contained"].AsArray().First(x => x["resourceType"]!.ToString().Equals("Patient"))["identifier"][0]["value"] = evaluation.EvaluationId.ToString();
        
        TestContext.WriteLine($"[{TestContext.TestName}]EvaluationID: " + evaluation.EvaluationId);
        
        var answersDict = GenerateKedPerformedAnswers();
        
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
        
        var egfrExam = await GetExamByEvaluationId(evaluation.EvaluationId);
        var examId = egfrExam.ExamId;
        
        var labsWebhookInbound = await CoreKafkaActions.GetLabsWebhookResultEvent<LabWebhookResultEvent>(createResultResponse.RequestId);
        labsWebhookInbound.EventId.Should().Be(createResultResponse.RequestId);
        labsWebhookInbound.TestName.Should().Be(ValidExamType);
        labsWebhookInbound.Vendor.Should().Be(LgcVendorName);
        
        var egfrLabResults = await GetLabResultByExamId(examId);
        egfrLabResults.EgfrResult.Should().Be(null);
        egfrLabResults.ResultDescription.Should().Be(description);
        egfrLabResults.NormalityIndicatorId.Should().Be(normalityIndicator);
        egfrLabResults.ReceivedDate.Should().Be(DateTime.MinValue);
        
        var egfrResultEvent = await CoreKafkaActions.GetEgfrResultsReceivedEvent<ResultsReceived>(evaluation.EvaluationId);
        egfrResultEvent.Result.Result.Should().Be(null);
        egfrResultEvent.Result.AbnormalIndicator.Should().Be(normality);
        egfrResultEvent.IsBillable.Should().BeFalse();
        egfrResultEvent.Determination.Should().Be(normality);
        
    }

    [TestMethod]
    public async Task ANC_T1203_InternalLabResultsE2ENullEvalTest()
    {
        var resultObject = GetResults(65);

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
    public async Task ANC_T1204_InternalLabResultsE2ENullPatientResourceTest()
    {
        var resultObject = GetResults(65);

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
    public async Task ANC_T1205_InternalLabResultsE2ENullResultResourceTest()
    {
        var (_, _, evaluation) = await CoreApiActions.PrepareEvaluation();

        var resultObject = GetResults(65);
        
        resultObject!["entry"]![0]["resource"]["contained"].AsArray().First(x => x["resourceType"]!.ToString().Equals("Patient"))["identifier"][0]["value"] = evaluation.EvaluationId.ToString();
        resultObject!["entry"]![0]["resource"]["contained"].AsArray()
            .Remove(resultObject!["entry"]![0]["resource"]["contained"].AsArray()
                    .Where(x=>x["resourceType"]!.ToString().Equals("Observation"))
                    .First(x=>x!["code"]["text"].ToString().Equals("Estimated Glomerular Filtration Rate")));

        TestContext.WriteLine($"[{TestContext.TestName}] Result with No Result");
        
        var answersDict = GenerateKedPerformedAnswers();

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
    public async Task ANC_T1206_InternalLabResultsE2ENullResultDescriptionTest()
    {
        var (_, _, evaluation) = await CoreApiActions.PrepareEvaluation();

        var resultObject = GetResults(65);

        resultObject!["entry"]![0]["resource"]["contained"].AsArray().First(x => x["resourceType"]!.ToString().Equals("Patient"))["identifier"][0]["value"] = evaluation.EvaluationId.ToString();
        resultObject!["entry"]![0]["resource"]["contained"].AsArray()
                .Where(x=>x["resourceType"]!.ToString().Equals("Observation")).First(x=>x["code"]!["text"].ToString().Equals("Estimated Glomerular Filtration Rate"))["note"][0]["text"]=null;

        TestContext.WriteLine($"[{TestContext.TestName}] Result with No Result");
        
        var answersDict = GenerateKedPerformedAnswers();

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
    public async Task ANC_T1207_InternalLabResultsE2EInvalidResultUnit()
    {
        var (_, _, evaluation) = await CoreApiActions.PrepareEvaluation();
        
        // Add results with invalid units
        var resultObject = GetResults(65);
        
        resultObject!["entry"]![0]["resource"]["contained"].AsArray().First(x => x["resourceType"]!.ToString().Equals("Patient"))["identifier"][0]["value"] = evaluation.EvaluationId.ToString();
        resultObject!["entry"]![0]["resource"]["contained"].AsArray().Where(x=>x["resourceType"]!.ToString().Equals("Observation")).First(x=>x!["code"]["text"].ToString().Equals("Estimated Glomerular Filtration Rate"))["valueQuantity"]["code"] = "mg/dL";
        resultObject!["entry"]![0]["resource"]["contained"].AsArray().Where(x=>x["resourceType"]!.ToString().Equals("Observation")).First(x=>x!["code"]["text"].ToString().Equals("Estimated Glomerular Filtration Rate"))["valueQuantity"]["unit"] = "mg/dL";
        
        TestContext.WriteLine($"[{TestContext.TestName}]EvaluationID: " + evaluation.EvaluationId);
        
        var answersDict = GenerateKedPerformedAnswers();
        
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
    
    
    private static JsonNode GetResults(double egfrResult)
    {
        const string resultFilePath = "../../../../Signify.eGFR.System.Tests.Core/Data/sampleFhirResult.json";
        var resultObject = JsonNode.Parse(File.ReadAllText(resultFilePath));
        
        resultObject!["entry"]![0]["resource"]["contained"].AsArray().Where(x=>x["resourceType"]!.ToString().Equals("Observation")).First(x=>x!["code"]["text"].ToString().Equals("Estimated Glomerular Filtration Rate"))["valueQuantity"]["value"] = egfrResult.ToString();
        return resultObject;
    }
    
    private static JsonNode GetResultsUndetermined(string description)
    {
        const string resultFilePath = "../../../../Signify.eGFR.System.Tests.Core/Data/sampleFhirUndeterminedResult.json";
        var resultObject = JsonNode.Parse(File.ReadAllText(resultFilePath));

        resultObject!["entry"]![0]["resource"]["contained"]
            .AsArray()
            .Where(x => x["resourceType"]!.ToString().Equals("Observation"))
            .First(x => x!["code"]["coding"]!.AsArray().Any(c => c["code"]!.ToString().Equals(Signify.eGFR.Core.Constants.Fhir.GlomerularFiltrationRate)))["interpretation"]![0]!["text"] = description;
        return resultObject;
    }
    
    private static string GetVendorAuthResponseBody()
    {
        const string filePath = "../../../../Signify.eGFR.System.Tests.Core/Data/vendorAuthResponseBody.json";
        var resultObject = JsonNode.Parse(File.ReadAllText(filePath));
        return resultObject!.ToJsonString();
    }
    
    
}