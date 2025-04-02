using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Messages.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.Messages.Queries;

public class GetExamRecord : IRequest<ExamModel>
{
    public GetExamRecord()
    {
    }

    public int? ExamId { get; set; }
    public long? EvaluationId { get; set; }
    public long? MemberPlanId { get; set; }
    public DateTimeOffset? DateOfService { get; set; }

    #region Formatting
    public override string ToString()
        => $"{nameof(ExamId)}: {ExamId}, {nameof(EvaluationId)}: {EvaluationId}, {nameof(MemberPlanId)}: {MemberPlanId}, {nameof(DateOfService)}: {DateOfService}";

    private bool Equals(GetExamRecord other)
        => ExamId == other.ExamId && EvaluationId == other.EvaluationId && MemberPlanId == other.MemberPlanId && DateOfService.Equals(other.DateOfService);

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((GetExamRecord)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = ExamId.GetHashCode();
            hashCode = (hashCode * 397) ^ EvaluationId.GetHashCode();
            hashCode = (hashCode * 397) ^ MemberPlanId.GetHashCode();
            hashCode = (hashCode * 397) ^ DateOfService.GetHashCode();
            return hashCode;
        }
    }
    #endregion
}

public class GetExamHandler(ILogger<GetExamHandler> log, DataContext context, IMapper mapper)
    : IRequestHandler<GetExamRecord, ExamModel>
{
    [Trace]
    public Task<ExamModel> Handle(GetExamRecord request, CancellationToken cancellationToken)
    {
        log.LogDebug("{Request} -- Exam lookup", request);

        var exam = context.Exams.Include(e => e.EvaluationObjective).FirstOrDefault(e => e.MemberPlanId == request.MemberPlanId && e.DateOfService == request.DateOfService);

        if (exam == null && request.ExamId != null)
            exam = context.Exams.Include(e => e.EvaluationObjective).FirstOrDefault(e => e.ExamId == request.ExamId);

        if (exam == null && request.EvaluationId != null)
            exam = context.Exams.Include(e => e.EvaluationObjective).SingleOrDefault(e => e.EvaluationId == request.EvaluationId);

        if (exam != null)
        {
            log.LogDebug("ExamId: {ExamId}, EvaluationId: {EvaluationId} -- Exam record found", exam.ExamId, exam.EvaluationId);
        }
        else
        {
            exam = new Exam();
            log.LogDebug("ExamId: {ExamId},--DEE exam record found", request.ExamId);
        }

        return Task.FromResult(mapper.Map<ExamModel>(exam));
    }
}