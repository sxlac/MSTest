using Signify.eGFR.System.Tests.Core.Actions;
using Signify.EvaluationsApi.Core.Values;
using Signify.QE.MSTest.Attributes;
using Signify.eGFR.System.Tests.Core.Constants;
using Signify.eGFR.Core.Events.Akka;
using Signify.eGFR.System.Tests.Core.Models.Kafka;

namespace Signify.eGFR.System.Tests.Tests;

[TestClass, TestCategory("regression")]
public class LabResultKafkaValidation : BaseTestActions
{
    public TestContext TestContext { get; set; }
    
    [RetryableTestMethod(2)]
    [DataRow("65", "N",true)]
    [DataRow("59", "A",true)]
    [DataRow("0", "U",false)]
    public async Task ANC_T1135_LabPerformedTest(string egfrResult, string normality, bool isBillable)
    
    {
         // Arrange
         var (member, appointment, evaluation) =
             await CoreApiActions.PrepareEvaluation();
         var answersDict = GenerateKedPerformedAnswers();
         // Act
         CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId,
             CoreApiActions.GetEvaluationAnswerList(answersDict));
         CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
         CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);

         // Assert
         // Database eGFR table
         var egfr = await GetExamByEvaluationId(evaluation.EvaluationId);
         var examId = egfr.ExamId;
         
         // Publish the homeaccess lab results
         var resultsReceivedValue = new HomeAccessLabResults()
         {
             EstimatedGlomerularFiltrationRateResultColor = "",
             EstimatedGlomerularFiltrationRateResultDescription = "abcd",
             EgfrResult = egfrResult,
             EvaluationId = evaluation.EvaluationId,
             DateLabReceived = DateTime.UtcNow,
         };
         CoreHomeAccessKafkaActions.PublishEgfrLabResultEvent(resultsReceivedValue,evaluation.EvaluationId.ToString());
         await Task.Delay(5000);
         
         // Validate the DB table for LabResults
         var labresult = await GetLabResultByExamId(examId);
         labresult.ExamId = examId;
        
    
         // Validate ResultsReceived Message for 'egfr_status' Event published in Kafka
         var examResultEvent = await CoreKafkaActions.GetEgfrResultsReceivedEvent<ResultsReceived>(evaluation.EvaluationId);
         examResultEvent.Result.AbnormalIndicator.Should().Be(normality);
         examResultEvent.Result.Description.Should().Be("abcd");
         examResultEvent.Determination.Should().Be(normality);
         examResultEvent.IsBillable.Should().Be(isBillable);
         examResultEvent.ProductCode.Should().Be(TestConstants.Product);
         examResultEvent.PerformedDate.Should().BeSameDateAs(DateTimeOffset.Parse(answersDict[Answers.DosAnswerId]));
     }
    
}