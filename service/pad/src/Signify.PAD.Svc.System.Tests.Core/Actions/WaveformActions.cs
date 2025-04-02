namespace Signify.PAD.Svc.System.Tests.Core.Actions;

public class WaveformActions : BaseTestActions
{
    protected const string SourcePdf = "../../../../Signify.PAD.Svc.System.Tests.Core/SamplePdf/SamplePADWaveform.pdf";
    protected const string IncomingFolder = "../../../file_mount/Incoming/SemlerScientific/";
    protected const string PendingFolder = "../../../file_mount/Pending/SemlerScientific/";
    protected readonly string ProcessedFolder = $"../../../file_mount/Processed/SemlerScientific/ClientId/14/{DateTime.Now.Year}/{DateTime.Now.Month}/";
    protected const string FailedFolder = "../../../file_mount/Failed/FileAlreadyUploaded/SemlerScientific/";
    protected string GetFileName(string lastName, long memberPlanId)
    {
        return $"{lastName}_{memberPlanId}_PAD_BL_{DateTime.Now.ToString("MMddyy")}.PDF";
    }
}