using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.HBA1CPOC.Messages.Events;
using Signify.HBA1CPOC.Svc.Core.Constants;
using Signify.HBA1CPOC.Svc.Core.Data;
using Signify.HBA1CPOC.Svc.Core.Events;
using Signify.HBA1CPOC.Svc.Core.Infrastructure.Observability;
using Signify.HBA1CPOC.Svc.Core.Queries;

namespace Signify.HBA1CPOC.Svc.Core.Commands;

/// <summary>
/// This handles Evaluation Received Event and raise HbA1cPoc Performed Event.
/// </summary>
[ExcludeFromCodeCoverage]
public class CreateHbA1CPoc : ICommand
{
    public Guid Id { get; set; }
    public int EvaluationId { get; set; }
    public int EvaluationTypeId { get; set; }
    public int FormVersionId { get; set; }
    public int? ProviderId { get; set; }
    public string UserName { get; set; }
    public int AppointmentId { get; set; }
    public string ApplicationId { get; set; }
    public int MemberPlanId { get; set; }
    public int MemberId { get; set; }
    public int ClientId { get; set; }
    public string DocumentPath { get; set; }
    public DateTimeOffset CreatedDateTime { get; set; }
    public DateTime ReceivedDateTime { get; set; }
    public DateTime? DateOfService { get; set; }
    public List<Product> Products { get; set; }
    public Location Location { get; set; }
}

public class CreateHbA1CPocHandler : IHandleMessages<CreateHbA1CPoc>
{
    private readonly ILogger<CreateHbA1CPocHandler> _logger;
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;
    private readonly Hba1CpocDataContext _dataContext;
    private readonly IPublishObservability _publishObservability;

    public CreateHbA1CPocHandler(ILogger<CreateHbA1CPocHandler> logger,
        IMapper mapper,
        Hba1CpocDataContext dataContext,
        IMediator mediator,
        IPublishObservability publishObservability)
    {
        _logger = logger;
        _mapper = mapper;
        _mediator = mediator;
        _dataContext = dataContext;
        _publishObservability = publishObservability;
    }

    [Transaction]
    public async Task Handle(CreateHbA1CPoc message, IMessageHandlerContext context)
    {
        //Query database to check if HBA1CPOC exists.
        var entity = await _dataContext.HBA1CPOC.FirstOrDefaultAsync(s => s.EvaluationId == message.EvaluationId, context.CancellationToken);
        if (entity != null)
        {
            await UpdateDateOfService(message.DateOfService, entity, message.EvaluationId, context);
            PublishObservability(entity, message.EvaluationId, Observability.Evaluation.EvaluationClarificationEvent);
            return;
        }

        //Query Evaluation api and filter eval answers
        var result = await _mediator.Send(new CheckHBA1CPOCEval { EvaluationId = message.EvaluationId }, context.CancellationToken);

        var isLabPerformed = result.IsHBA1CEvaluation;

        _logger.LogInformation("Lab {WasPerformed} for EvaluationId={EvaluationId}, EventId={EventId}",
            isLabPerformed ? "was" : "was not", message.EvaluationId, message.Id);

        //Raise HBA1CPOC Evaluation Received NService bus event
        var evalReceived = _mapper.Map<EvalReceived>(message);
        evalReceived.IsLabPerformed = isLabPerformed;

        await context.Publish(evalReceived);
    }

    /// <summary>
    /// This method compares DOS and updated the product when different
    /// </summary>
    public async Task UpdateDateOfService(DateTime? eventDos, Data.Entities.HBA1CPOC entity,
        int evaluationId, IMessageHandlerContext context)
    {
        if (!eventDos.HasValue)
        {
            _logger.LogInformation("EvaluationId={EvaluationId} - Nothing to update for this exam because DateOfService is null", entity.EvaluationId);
            return;
        }

        if (eventDos == entity.DateOfService)
        {
            _logger.LogInformation("EvaluationId={EvaluationId} - Nothing to update for this exam because DateOfService has not changed: {DateOfService}",
                entity.EvaluationId, entity.DateOfService);
        }

        var priorDateOfService = entity.DateOfService;
        var dosUpdate = new DateOfServiceUpdated(evaluationId, eventDos.Value);
        await context.Send(dosUpdate);
        _logger.LogInformation("EvaluationId={EvaluationId} - DOS updated to {UpdatedDateOfService}; previous DOS {PriorDateOfService}",
            entity.EvaluationId, entity.DateOfService, priorDateOfService);

        PublishObservability(entity, evaluationId, Observability.Evaluation.EvaluationDosUpdatedEvent);
    }

    private void PublishObservability(Data.Entities.HBA1CPOC entity, int evaluationId, string eventType)
    {
        var observabilityDosUpdatedEvent = new ObservabilityEvent
        {
            EvaluationId = evaluationId,
            EventType = eventType,
            EventValue = new Dictionary<string, object>
            {
                { Observability.EventParams.EvaluationId, evaluationId },
                { Observability.EventParams.CreatedDateTime, entity.CreatedDateTime.ToUnixTimeSeconds() }
            }
        };

        _publishObservability.RegisterEvent(observabilityDosUpdatedEvent, true);
    }
}