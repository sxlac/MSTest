using Newtonsoft.Json.Linq;
using Signify.EvaluationsApi.Core.Values;
using Signify.FOBT.Svc.System.Tests.Core.Actions;
using Signify.QE.MSTest.Attributes;
using Signify.QE.MSTest.Utilities;
using Signify.FOBT.Svc.System.Tests.Core.Models.Kafka;
using Signify.Dps.Test.Utilities.DataGen;
using Signify.FOBT.Svc.System.Tests.Core.Constants;

namespace Signify.FOBT.Svc.System.Tests.Tests;

[TestClass, TestCategory("regression")]
public class ProviderPayTests : ProviderPayActions
{
    [RetryableTestMethod(2)]
    [DataRow("Positive", "A", "")]
    [DataRow("Negative", "N", "")]
    [DataRow("Negative", "U", "Results Expired")]
    
    public async Task ANC_T620_ProviderPayTest(string labresult, string normality, string exception)
    {
        // Arrange
        var (member, appointment, evaluation) =
            await CoreApiActions.PrepareEvaluation();
        var answersDict = GeneratePerformedAnswers();


        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId, CoreApiActions.GetEvaluationAnswerList(answersDict));
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        
        // Database FOBT table
        var fobt = await GetFOBTByEvaluationId(evaluation.EvaluationId, 20, 5);

        // Publish the homeaccess lab results
        var resultsReceivedValue = new HomeAccessLabResults()
        {
            EventId = DataGen.NewUuid(),
            CreatedDateTime= DateTime.Now,
            OrderCorrelationId = fobt.OrderCorrelationId,
            Barcode = answersDict[Answers.Barcode],
            LabTestType= "FOBT",
            LabResults = labresult,
            AbnormalIndicator = normality,
            Exception = exception,
            CollectionDate = DateTime.Now,
            ServiceDate = DateTime.Now,
            ReleaseDate = DateTime.Now
        };
        CoreHomeAccessKafkaActions.PublishEvent<HomeAccessLabResults>("homeaccess_labresults",resultsReceivedValue,evaluation.EvaluationId.ToString(),"HomeAccessResultsReceived");
        await Task.Delay(5000);

        if (normality == "U")
        {
            await ValidateProviderNonPayable(evaluation.EvaluationId);
        }
        else
        {
            await ValidateProviderPayable(evaluation.EvaluationId, member.MemberPlanId);
        }
    }
    
}