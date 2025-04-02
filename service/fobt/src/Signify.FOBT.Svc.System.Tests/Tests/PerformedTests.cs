using FluentResults;
using Newtonsoft.Json.Linq;
using Signify.EvaluationsApi.Core.Values;
using Signify.FOBT.Svc.Core.Constants;
using Signify.FOBT.Svc.Core.Events;
using Signify.FOBT.Svc.System.Tests.Core.Actions;
using Signify.QE.MSTest.Attributes;
using Signify.QE.MSTest.Utilities;
using Signify.Dps.Test.Utilities.DataGen;
using Signify.FOBT.Svc.System.Tests.Core.Constants;
using Signify.FOBT.Svc.System.Tests.Core.Models.Kafka;

namespace Signify.FOBT.Svc.System.Tests.Tests;

[TestClass, TestCategory("regression")]
public class PerformedTests: PerformedActions
{
    [RetryableTestMethod]
    public async Task ANC_T344_Performed()
    {
        // Arrange
        var (member, appointment, evaluation) =
            await CoreApiActions.PrepareEvaluation();
        var answersDict = GeneratePerformedAnswers();

        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId,
            CoreApiActions.GetEvaluationAnswerList(answersDict));
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        
        // Assert
        // Database FOBT table
        var fobt = await GetFOBTByEvaluationId(evaluation.EvaluationId, 20, 5);
        fobt.MemberPlanId.Should().Be(Convert.ToInt32(member.MemberPlanId));
        fobt.MemberId.Should().Be(Convert.ToInt32(member.MemberId));
        fobt.AppointmentId.Should().Be(appointment.AppointmentId);
        fobt.ProviderId.Should().Be(Provider.ProviderId);
        fobt.CenseoId.Should().Be(member.CenseoId);
        fobt.ClientId.Should().Be(member.ClientID);
        fobt.City.Should().Be(member.City);
        fobt.State.Should().Be(member.State);
        fobt.AddressLineOne.Should().Be(member.AddressLineOne);
        fobt.AddressLineTwo.Should().Be(member.AddressLineTwo);
        fobt.ZipCode.Should().Be(member.ZipCode);
        fobt.NationalProviderIdentifier.Should().Be(Provider.NationalProviderIdentifier);
        fobt.FirstName.Should().Be(member.FirstName);
        fobt.LastName.Should().Be(member.LastName);
        fobt.MiddleName.Should().Be(member.MiddleName);
        fobt.DateOfBirth.Should().Be(member.DateOfBirth);
        fobt.Barcode.Should().Be(answersDict[Answers.Barcode]);
        fobt.OrderCorrelationId.Should().NotBeEmpty();
        fobt.DateOfService.Should().Be(DateTime.Parse(answersDict[Answers.DosAnswerId]).Date);
        
        // Database status updates
        var expectedIds = new List<int>
        {
            ExamStatusCodes.ExamPerformed.FOBTStatusCodeId
        };
        
        var finalizedEvent =
            await CoreKafkaActions.GetEvaluationEvent<EvaluationFinalizedEvent>(evaluation.EvaluationId,
                "EvaluationFinalizedEvent");
        await ValidateExamStatusCodesByEvaluationId(evaluation.EvaluationId, expectedIds, 10, 3);
        
        // Kafka performed status event
        var performed = await CoreKafkaActions.GetPerformedStatusEvent<PerformedEvent>(evaluation.EvaluationId);
        performed.MemberPlanId.Should().Be(member.MemberPlanId);
        performed.ProviderId.Should().Be(Provider.ProviderId);
        performed.Barcode.Should().Be(answersDict[Answers.Barcode]);
        performed.EvaluationId.Should().Be(evaluation.EvaluationId);
        performed.ProductCode.Should().Be(TestConstants.Product);
        performed.CreatedDate.Date.Should().Be(finalizedEvent.CreatedDateTime.Date);
        performed.ReceivedDate.Date.Should().Be(finalizedEvent.ReceivedDateTime.Date);
    }

    [RetryableTestMethod]
    [DataRow("Positive", "A")]
    public async Task ANC_T730_PerformedWithBarcodeUpdate(string labresult, string normality)
    {
        // Arrange
        var (member, appointment, evaluation) =
            await CoreApiActions.PrepareEvaluation();
        var answersDict = GeneratePerformedAnswers();

        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId,
            CoreApiActions.GetEvaluationAnswerList(answersDict));
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);

        // Assert
        // Database FOBT table
        var fobt = await GetFOBTByEvaluationId(evaluation.EvaluationId, 20, 5);
        
        fobt.Barcode.Should().Be(answersDict[Answers.Barcode]);
        fobt.OrderCorrelationId.Should().NotBeEmpty();
        var  orderCorrelationId= fobt.OrderCorrelationId;
        var examId = fobt.FOBTId;
        
        // Search the barcode history entry in the FOBTBarcodeHistory table. There should be no entry for this new evaluation.
        var barcodeHistory = await getBarcodeHistoryResultByFOBTId(examId, 2, 5);
        barcodeHistory.Should().BeNull();

        // Generating new random barcode to publish to the labs_barcode topic
        var newRandomBarcode = DataGen.RandomInt(100000,999999).ToString();
        
        // Publish the BarcodeUpdate event to the labs_barcode topic using newly created barcode & OrderCorrelationId 
        var barcodeUpdateEventValue = new BarcodeUpdateEvent()
        {
            MemberPlanId = member.MemberPlanId,
            EvaluationId = evaluation.EvaluationId,
            ProductCode = "FOBT",
            OrderCorrelationId = DataGen.NewUuid(),
            Barcode = newRandomBarcode
        };
        CoreKafkaActions.PublishEvent<BarcodeUpdateEvent>("labs_barcode",barcodeUpdateEventValue,evaluation.EvaluationId.ToString(),"BarcodeUpdate");
        await Task.Delay(5000);

        // Check DB “FOBT” table contains updated barcode for this evaluation
        fobt.Barcode = newRandomBarcode;
        
        // Check DB “FOBTBarcodeHistory” table contains entry for old barcode 
        var getBarcodeHistory = await getBarcodeHistoryResultByFOBTId(examId, 2, 5);
        getBarcodeHistory.Barcode.Should().Be(answersDict[Answers.Barcode]);
        getBarcodeHistory.OrderCorrelationId.ToString().Should().Be(orderCorrelationId.ToString());
        
        // Publish the homeaccess lab results
        var resultsReceivedValue = new HomeAccessLabResults()
        {
            EventId = DataGen.NewUuid(),
            CreatedDateTime= DateTime.Now,
            OrderCorrelationId = orderCorrelationId,
            Barcode = answersDict[Answers.Barcode],
            LabTestType= "FOBT",
            LabResults = labresult,
            AbnormalIndicator = normality,
            Exception = "",
            CollectionDate = DateTime.Now,
            ServiceDate = DateTime.Now,
            ReleaseDate = DateTime.Now
        };
        CoreHomeAccessKafkaActions.PublishEvent<HomeAccessLabResults>("homeaccess_labresults",resultsReceivedValue,evaluation.EvaluationId.ToString(),"HomeAccessResultsReceived");
        await Task.Delay(5000);
        
        // Get the lab results from the database and verify they match the results file
        var labResult = await getLabResultsByFOBTId(examId, 20, 5);

        labResult.OrderCorrelationId.ToString().Should().Match(orderCorrelationId.ToString());
        labResult.Barcode.Should().Be(answersDict[Answers.Barcode]);
        labResult.AbnormalIndicator.Should().Be(normality);
        labResult.LabResult.Should().Be(labresult);

        // Validate that the Kafka event for the results are as expected
        var results = await CoreKafkaActions.GetExamResultEvent<GetExamResultEvent>(evaluation.EvaluationId, "Results");
        
        results.EvaluationId.Should().Be(evaluation.EvaluationId);
        results.PerformedDate.Date.Should().Be(resultsReceivedValue.ServiceDate.Date);
        results.ReceivedDate.Date.Should().Be(resultsReceivedValue.ReleaseDate.Date);
        results.MemberCollectionDate.Date.Should().Be(resultsReceivedValue.CollectionDate.Date);
        results.Determination.Should().Be(normality);
        results.Barcode.Should().Be(resultsReceivedValue.Barcode);
        results.IsBillable.Should().BeTrue();
        var resultDetails = results.Result[0];
        resultDetails.Result.Should().Be(labresult);
        resultDetails.Exception.Should().Be("");
        resultDetails.AbnormalIndicator.Should().Be(normality);
        
    }
    
    [RetryableTestMethod]
    [DataRow("Negative", "N")]
    public async Task ANC_T667_PerformedWithBarcodeUpdate(string labresult, string normality)
    {
        // Arrange
        var (member, appointment, evaluation) =
            await CoreApiActions.PrepareEvaluation();
        var answersDict = GeneratePerformedAnswers();

        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId,
            CoreApiActions.GetEvaluationAnswerList(answersDict));
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);

        // Assert
        // Database FOBT table
        var fobt = await GetFOBTByEvaluationId(evaluation.EvaluationId, 20, 5);
        
        fobt.Barcode.Should().Be(answersDict[Answers.Barcode]);
        fobt.OrderCorrelationId.Should().NotBeEmpty();
        var  orderCorrelationId= fobt.OrderCorrelationId;
        var examId = fobt.FOBTId;
        
        // Search the barcode history entry in the FOBTBarcodeHistory table. There should be no entry for this new evaluation.
        var barcodeHistory = await getBarcodeHistoryResultByFOBTId(examId, 2, 5);
        barcodeHistory.Should().BeNull();
        
        // Publish the homeaccess lab results
        var resultsReceivedValue = new HomeAccessLabResults()
        {
            EventId = DataGen.NewUuid(),
            CreatedDateTime= DateTime.Now,
            OrderCorrelationId = orderCorrelationId,
            Barcode = answersDict[Answers.Barcode],
            LabTestType= "FOBT",
            LabResults = labresult,
            AbnormalIndicator = normality,
            Exception = "",
            CollectionDate = DateTime.Now,
            ServiceDate = DateTime.Now,
            ReleaseDate = DateTime.Now
        };
        CoreHomeAccessKafkaActions.PublishEvent<HomeAccessLabResults>("homeaccess_labresults",resultsReceivedValue,evaluation.EvaluationId.ToString(),"HomeAccessResultsReceived");
        await Task.Delay(5000);
        
        // Get the lab results from the database and verify they match the results file
        var labResult = await getLabResultsByFOBTId(examId, 20, 5);

        labResult.OrderCorrelationId.ToString().Should().Match(orderCorrelationId.ToString());
        labResult.Barcode.Should().Be(answersDict[Answers.Barcode]);
        labResult.AbnormalIndicator.Should().Be(normality);
        labResult.LabResult.Should().Be(labresult);

        // Validate that the Kafka event for the results are as expected
        var results = await CoreKafkaActions.GetExamResultEvent<GetExamResultEvent>(evaluation.EvaluationId, "Results");
        
        results.EvaluationId.Should().Be(evaluation.EvaluationId);
        results.PerformedDate.Date.Should().Be(resultsReceivedValue.ServiceDate.Date);
        results.ReceivedDate.Date.Should().Be(resultsReceivedValue.ReleaseDate.Date);
        results.MemberCollectionDate.Date.Should().Be(resultsReceivedValue.CollectionDate.Date);
        results.Determination.Should().Be(normality);
        results.Barcode.Should().Be(resultsReceivedValue.Barcode);
        results.IsBillable.Should().BeTrue();
        var resultDetails = results.Result[0];
        resultDetails.Result.Should().Be(labresult);
        resultDetails.Exception.Should().Be("");
        resultDetails.AbnormalIndicator.Should().Be(normality);
        
        // Generating new random barcode to publish to the labs_barcode topic
        var newRandomBarcode = DataGen.RandomInt(100000,999999).ToString();
        
        // Publish the BarcodeUpdate event to the labs_barcode topic using newly created barcode & OrderCorrelationId 
        var barcodeUpdateEventValue = new BarcodeUpdateEvent()
        {
            MemberPlanId = member.MemberPlanId,
            EvaluationId = evaluation.EvaluationId,
            ProductCode = "FOBT",
            OrderCorrelationId = DataGen.NewUuid(),
            Barcode = newRandomBarcode
        };
        CoreKafkaActions.PublishEvent<BarcodeUpdateEvent>("labs_barcode",barcodeUpdateEventValue,evaluation.EvaluationId.ToString(),"BarcodeUpdate");
        await Task.Delay(5000);

        // Check DB “FOBT” table contains updated barcode for this evaluation
        fobt.Barcode = newRandomBarcode;
        
        // Check DB “FOBTBarcodeHistory” table contains entry for old barcode 
        var getBarcodeHistory = await getBarcodeHistoryResultByFOBTId(examId, 2, 5);
        getBarcodeHistory.Barcode.Should().Be(answersDict[Answers.Barcode]);
        getBarcodeHistory.OrderCorrelationId.ToString().Should().Be(orderCorrelationId.ToString());
    }
}
