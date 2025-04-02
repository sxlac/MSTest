using System;
using System.Collections.Generic;
using Signify.DEE.Svc.Core.Messages.Commands;

#nullable disable
namespace Signify.DEE.Svc.Core.Data.Entities;

public partial class Exam
{
    public Exam()
    {
        ExamImages = new HashSet<ExamImage>();
        ExamLateralityGrades = new HashSet<ExamLateralityGrade>();
        ExamResults = new HashSet<ExamResult>();
        ExamStatuses = new HashSet<ExamStatus>();
    }

    public int ExamId { get; set; }
    public string ExamLocalId { get; set; }
    public long? EvaluationId { get; set; }
    public long MemberPlanId { get; set; }
    public int ProviderId { get; set; }
    public DateTimeOffset DateOfService { get; set; }
    public bool? Gradeable { get; set; }
    public DateTimeOffset CreatedDateTime { get; set; }
    public DateTimeOffset? ReceivedDateTime { get; set; }
    public string State { get; set; }
    public Guid? RequestId { get; set; }
    public virtual ICollection<ExamImage> ExamImages { get; set; }
    public virtual ICollection<ExamResult> ExamResults { get; set; }
    public virtual ICollection<ExamStatus> ExamStatuses { get; set; }
    public virtual ICollection<ExamLateralityGrade> ExamLateralityGrades { get; set; }
    public virtual ProviderPay ProviderPay { get; set; }
    public virtual EvaluationObjective EvaluationObjective { get; set; }
    public int EvaluationObjectiveId { get; set; }
    public int? ClientId { get; set; }
    public long? AppointmentId { get; set; }

    public string RetinalImageTestingNotes { get; set; }
    public bool? HasEnucleation { get; set; }

    public static Exam Create(CreateExamRecord request, DateTimeOffset createdDateTime)
    {
        // ANC-4562 - Tech debt, all ProviderId properties should be `int` instead of `string`
        var providerId = int.Parse(request.ProviderId);

        var exam = new Exam
        {
            MemberPlanId = request.MemberPlanId,
            ProviderId = providerId,
            DateOfService = request.DateOfService,
            State = request.State,
            RequestId = request.RequestId,
            Gradeable = request.Gradable,
            CreatedDateTime = createdDateTime == DateTime.MinValue ? DateTime.UtcNow : createdDateTime,
            ExamLocalId = Guid.NewGuid().ToString(),
            ReceivedDateTime = request.ReceivedDateTime == DateTime.MinValue ? DateTime.UtcNow : request.ReceivedDateTime,
            ClientId = request.ClientId,
            EvaluationObjectiveId = request.EvaluationObjective.EvaluationObjectiveId,
            AppointmentId = request.AppointmentId,
            RetinalImageTestingNotes = request.RetinalImageTestingNotes,
            HasEnucleation = request.HasEnucleation
        };

        if (request.EvaluationId.HasValue)
        {
            exam.EvaluationId = request.EvaluationId.Value;
        }

        return exam;
    }
}