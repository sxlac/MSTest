namespace Signify.DEE.Svc.System.Tests.Core.Models.Database;

public class Exam
{
    public int ExamId { get; set; }
    public int DeeExamId { get; set; }
    public long EvaluationId { get; set; }
    public long MemberPlanId { get; set; }
    public int ProviderId { get; set; }
    public DateTime DateOfService { get; set; }
    public bool Gradeable { get; set; }
    public DateTime CreatedDateTime { get; set; }
    public string State { get; set; }
    public Guid RequestId { get; set; }
    public int ClientId { get; set; }
    public DateTime ReceivedDateTime { get; set; }
    public string ExamLocalId { get; set; }
    public short EvaluationObjectiveId { get; set; }
    public long AppointmentId { get; set; }
    public string RetinalImageTestingNotes { get; set; }
}