using Signify.EvaluationsApi.Core.Values;
using Signify.PAD.Svc.System.Tests.Core.Constants;

namespace Signify.PAD.Svc.System.Tests.Tests;

[TestClass, TestCategory("waveform")]
public class WaveformTests : WaveformActions
{
    public TestContext TestContext { get; set; }
    
    [RetryableTestMethod]
    public async Task ANC_T362_WaveformDoc_Processed_For_Performed()
    {
        // Arrange
        var (member, appointment, evaluation) = await CoreApiActions.PrepareEvaluation();
        var answersDict = GeneratePerformedAnswers("99","1");
        
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId, CoreApiActions.GetEvaluationAnswerList(answersDict));
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        
        var fileName = GetFileName(member.LastName, member.MemberPlanId);
        
        TestContext.WriteLine($"[{TestContext.TestName}] Waveform file: {fileName}");
        
        FileShareActions.WriteFile(SourcePdf, $"{IncomingFolder}/{fileName}");
        
        // Assert
        (await FileShareActions.CheckFileExists(fileName, PendingFolder, 150)).Should().Be(true);
        (await FileShareActions.CheckFileExists(fileName, ProcessedFolder, 150)).Should().Be(true);

        var waveformDoc = await GetWaveformDocumentResultsByMemberPlanId(member.MemberPlanId, 50, 3);
        waveformDoc.Filename.Should().Be(fileName);
        
        var expectedIds = new List<int>
        {
            ExamStatusCodes.WaveformDocumentDownloaded.PADStatusCodeId,
            ExamStatusCodes.WaveformDocumentUploaded.PADStatusCodeId
            
        };
        
        await ValidateExamStatusCodesByEvaluationId(evaluation.EvaluationId, expectedIds, 15, 2);
        
        // Validate evaluation document from evaluation api
        CoreApiActions.GetEvaluationDocument(evaluation.EvaluationId);
    }
    
    [RetryableTestMethod]
    public async Task ANC_T366_WaveformDoc_Processed_For_NotPerformed()
    {
        // Arrange
        var (member, appointment, evaluation) = await CoreApiActions.PrepareEvaluation();
        var answersDict = GenerateNotPerformedAnswers();
        
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId, CoreApiActions.GetEvaluationAnswerList(answersDict));
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        
        var fileName = GetFileName(member.LastName, member.MemberPlanId);
        
        TestContext.WriteLine($"[{TestContext.TestName}] Waveform file: {fileName}");
        
        FileShareActions.WriteFile(SourcePdf, $"{IncomingFolder}/{fileName}");
        
        // Assert
        (await FileShareActions.CheckFileExists(fileName, PendingFolder, 150)).Should().Be(true);
        (await FileShareActions.CheckFileExists(fileName, ProcessedFolder, 150)).Should().Be(true);

        var waveformDoc = await GetWaveformDocumentResultsByMemberPlanId(member.MemberPlanId, 50, 3);
        waveformDoc.Filename.Should().Be(fileName);
        
        var expectedIds = new List<int>
        {
            ExamStatusCodes.WaveformDocumentDownloaded.PADStatusCodeId,
            ExamStatusCodes.WaveformDocumentUploaded.PADStatusCodeId
            
        };
        
        await ValidateExamStatusCodesByEvaluationId(evaluation.EvaluationId, expectedIds, 15, 2);
        
        // Validate evaluation document from evaluation api
        CoreApiActions.GetEvaluationDocument(evaluation.EvaluationId);
    }
    
    [RetryableTestMethod]
    public async Task ANC_T369_WaveformDoc_Created_Before_Finalizing()
    {
        // Arrange
        var (member, appointment, evaluation) = await CoreApiActions.PrepareEvaluation();
        var answersDict = GeneratePerformedAnswers("99","1");
        
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId, CoreApiActions.GetEvaluationAnswerList(answersDict));
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        
        var fileName = GetFileName(member.LastName, member.MemberPlanId);
        
        TestContext.WriteLine($"[{TestContext.TestName}] Waveform file: {fileName}");
        
        FileShareActions.WriteFile(SourcePdf, $"{IncomingFolder}/{fileName}");
        
        // Assert
        (await FileShareActions.CheckFileExists(fileName, PendingFolder, 150)).Should().Be(true);
        (await FileShareActions.CheckFileExists(fileName, ProcessedFolder, 150)).Should().Be(false);
        
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        
        (await FileShareActions.CheckFileExists(fileName, ProcessedFolder, 150)).Should().Be(true);

        var waveformDoc = await GetWaveformDocumentResultsByMemberPlanId(member.MemberPlanId, 50, 3);
        waveformDoc.Filename.Should().Be(fileName);
        
        var expectedIds = new List<int>
        {
            ExamStatusCodes.WaveformDocumentDownloaded.PADStatusCodeId,
            ExamStatusCodes.WaveformDocumentUploaded.PADStatusCodeId
            
        };
        
        await ValidateExamStatusCodesByEvaluationId(evaluation.EvaluationId, expectedIds, 15, 2);
        
        // Validate evaluation document from evaluation api
        CoreApiActions.GetEvaluationDocument(evaluation.EvaluationId);
    }
    
    [RetryableTestMethod]
    public async Task ANC_T364_Duplicate_WaveformDoc_Moved_To_Failed_Folder()
    {
        // Arrange
        var (member, appointment, evaluation) = await CoreApiActions.PrepareEvaluation();
        var answersDict = GeneratePerformedAnswers("99","1");
        
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId, CoreApiActions.GetEvaluationAnswerList(answersDict));
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        
        var fileName = GetFileName(member.LastName, member.MemberPlanId);
        
        TestContext.WriteLine($"[{TestContext.TestName}] Waveform file: {fileName}");
        
        FileShareActions.WriteFile(SourcePdf, $"{IncomingFolder}/{fileName}");
        
        // Assert
        (await FileShareActions.CheckFileExists(fileName, PendingFolder, 150)).Should().Be(true);
        (await FileShareActions.CheckFileExists(fileName, ProcessedFolder, 150)).Should().Be(true);

        var waveformDoc = await GetWaveformDocumentResultsByMemberPlanId(member.MemberPlanId, 50, 3);
        waveformDoc.Filename.Should().Be(fileName);
        
        var expectedIds = new List<int>
        {
            ExamStatusCodes.WaveformDocumentDownloaded.PADStatusCodeId,
            ExamStatusCodes.WaveformDocumentUploaded.PADStatusCodeId
            
        };
        
        await ValidateExamStatusCodesByEvaluationId(evaluation.EvaluationId, expectedIds, 15, 2);
        
        // Add a duplicate pdf file
        FileShareActions.WriteFile(SourcePdf, $"{IncomingFolder}/{fileName}");
        
        // Validate that the file is moved to failed folder
        (await FileShareActions.CheckFileExists(fileName, PendingFolder, 150)).Should().Be(true);
        (await FileShareActions.CheckFileExistsWithPartialFilename(fileName.Split(".")[0], FailedFolder,  150)).Should().Be(true);

    }

}