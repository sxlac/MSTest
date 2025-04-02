using Signify.DEE.Svc.Core.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Signify.DEE.Svc.Core.Messages.Models;

public class ExamModel
{
    public ExamModel()
    {
    }

    public int ExamId { get; set; }
    public long EvaluationId { get; set; }
    public long MemberPlanId { get; set; }
    public int ProviderId { get; set; }
    public Guid? RequestId { get; set; }
    public DateTimeOffset DateOfService { get; set; }
    public string Status { get; set; }
    public string State { get; set; }
    public DateTimeOffset CreatedDateTime { get; set; }
    public ExamResultModel ExamResult => ExamResults.FirstOrDefault();
    public string ExamLocalId { get; set; }
    public List<ExamImageModel> ExamImages { get; set; } = new();
    public List<ExamStatusModel> ExamStatuses { get; set; } = new();
    public List<ExamResultModel> ExamResults { get; set; } = new();
    public int ClientId { get; set; }
    public long? AppointmentId { get; set; }
    public string RetinalImageTestingNotes { get; set; }
    public bool? HasEnucleation { get; set; }
    public EvaluationObjective EvaluationObjective { get; set; }

    public override string ToString()
        => $"{nameof(ExamId)}: {ExamId}, {nameof(EvaluationId)}: {EvaluationId}, {nameof(MemberPlanId)}: {MemberPlanId}, {nameof(ProviderId)}: {ProviderId}, {nameof(RequestId)}: {RequestId}, {nameof(DateOfService)}: {DateOfService}, {nameof(Status)}: {Status}, {nameof(State)}: {State}, {nameof(ExamResult)}: {ExamResult}, {nameof(ExamImages)}: {ExamImages}, {nameof(ExamStatuses)}: {ExamStatuses}, {nameof(ExamResults)}: {ExamResults}";

    protected bool Equals(ExamModel other)
        => ExamId == other.ExamId && EvaluationId == other.EvaluationId && MemberPlanId == other.MemberPlanId && ProviderId == other.ProviderId && RequestId.Equals(other.RequestId) && DateOfService.Equals(other.DateOfService) && string.Equals(Status, other.Status) && string.Equals(State, other.State) && Equals(ExamImages, other.ExamImages) && Equals(ExamStatuses, other.ExamStatuses) && Equals(ExamResults, other.ExamResults) && Equals(ExamLocalId, other.ExamLocalId);
}