using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.DEE.Svc.Core.Constants;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Infrastructure;
using Signify.DEE.Svc.Core.Messages.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;

namespace Signify.DEE.Svc.Core.Messages.Commands;

public class CreateExamRecord : IRequest<CreateExamRecordResponse>
{
    public int ExamId { get; set; }
    public long? EvaluationId { get; set; }
    public long MemberPlanId { get; set; }
    public string ProviderId { get; set; }
    public DateTimeOffset DateOfService { get; set; }
    public bool? Gradable { get; set; }
    public string State { get; set; }
    public Guid? RequestId { get; set; }
    public DateTimeOffset CreatedDateTime { get; set; }
    public DateTimeOffset ReceivedDateTime { get; set; }
    public int ClientId { get; set; }
    public long AppointmentId { get; set; }
    public string RetinalImageTestingNotes { get; set; }
    public bool? HasEnucleation { get; set; }
    public EvaluationObjective EvaluationObjective { get; set; }
}

public class CreateExamRecordResponse(ExamModel exam, bool isNew)
{
    public ExamModel Exam { get; } = exam;

    /// <summary>
    /// Whether this exam was just inserted, or if it already existed in the database
    /// </summary>
    public bool IsNew { get; } = isNew;
}

public class CreateExamRecordHandler(
    ILogger<CreateExamRecordHandler> logger,
    IMapper mapper,
    DataContext context,
    IApplicationTime applicationTime,
    IPublishObservability publishObservability)
    : IRequestHandler<CreateExamRecord, CreateExamRecordResponse>
{
    [Trace]
    public async Task<CreateExamRecordResponse> Handle(CreateExamRecord request, CancellationToken cancellationToken)
    {
        var exam = await context.Exams.FirstOrDefaultAsync(e => e.EvaluationId == request.EvaluationId, cancellationToken);

        var isNew = exam == null;
        if (isNew)
        {


            exam = Exam.Create(request, applicationTime.UtcNow());

            exam = (await context.Exams.AddAsync(exam, cancellationToken)).Entity;
        }
        else
        {
            PublishObservability(request, Observability.Evaluation.EvaluationClarificationEvent);

            if (exam.DateOfService != request.DateOfService)
            {
                logger.LogInformation("DOS updated for existing exam with EvaluationId={EvaluationId}", request.EvaluationId);

                exam.DateOfService = request.DateOfService;

                PublishObservability(request, Observability.Evaluation.EvaluationDosUpdatedEvent);
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        if (isNew)
            logger.LogInformation("Created new exam for EvaluationId={EvaluationId}, with ExamId={ExamId}", request.EvaluationId, exam.ExamId);

        var examModel = mapper.Map<ExamModel>(exam);

        return new CreateExamRecordResponse(examModel, isNew);
    }

    private void PublishObservability(CreateExamRecord request, string eventType)
    {
        var observabilityEvent = new ObservabilityEvent
        {
            EvaluationId = request.EvaluationId,
            EventType = eventType,
            EventValue = new Dictionary<string, object>
            {
                {Observability.EventParams.EvaluationId, request.EvaluationId},
                {
                    Observability.EventParams.CreatedDateTime,
                    request.CreatedDateTime.ToUnixTimeSeconds()
                }
            }
        };

        publishObservability.RegisterEvent(observabilityEvent, true);
    }
}