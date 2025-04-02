using Signify.QE.MSTest.Attributes;
using Signify.EvaluationsApi.Core.Values;
using Signify.HBA1CPOC.Svc.Core.Events;
using Signify.HBA1CPOC.System.Tests.Core.Constants;

namespace Signify.HBA1CPOC.System.Tests.Tests;

[TestClass,TestCategory("regression")]
public class SecondEvaluationTests : PerformedActions
{
       public TestContext TestContext { get; set; }

    [RetryableTestMethod]
    public async Task ANC_T324_Hba1cpocDosTest()
    {
        // Arrange
        var (member,appointment,evaluation) = await CoreApiActions.PrepareEvaluation();
        var dateStamp = DateTime.Now;
        
        var answers = GeneratePerformedAnswers();
        
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId, CoreApiActions.GetEvaluationAnswerList(answers));
         
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        // Assert
        var evaluationEvents = await CoreKafkaActions.GetEvaluationEvent<EvaluationFinalizedEvent>(evaluation.EvaluationId, 
            "EvaluationFinalizedEvent");
        
        var eval = CoreApiActions.GetEvaluation(evaluation.EvaluationId);
        eval.Status.Should().Be("Completed");
        
        // Validate the entry using EvaluationId in HBA1CPOC & HBA1CPOCStatus tables. Status 1 = HBA1CPOCPerformed 
        var performedRecord = GetHba1CpocRecordByEvaluationId(evaluation.EvaluationId);
        DateTime conversion = (DateTime)performedRecord.DateOfService!;
        var utcDos = conversion.ToUniversalTime();
        utcDos.Should().BeSameDateAs(dateStamp);
        
        
        var newdateStamp = DateTime.Now.AddDays(2); 
        var newAnswers = GeneratePerformedAnswers();
        newAnswers[Answers.DoSAnswerId] = newdateStamp.ToString("O"); 
        
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId, CoreApiActions.GetEvaluationAnswerList(newAnswers));
         
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        // Assert

        Thread.Sleep(3000); 
        var newperformedRecord = GetHba1CpocRecordByEvaluationId(evaluation.EvaluationId);
        var newconversion = (DateTime)newperformedRecord.DateOfService!;
        var newUtcDos = newconversion.ToUniversalTime();
        newUtcDos.Should().BeSameDateAs(newdateStamp);
    }
}