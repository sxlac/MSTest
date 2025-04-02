using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.FOBT.Messages.Events;
using Signify.FOBT.Svc.Core.Events;
using Signify.FOBT.Svc.Core.Queries;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Signify.FOBT.Svc.Core.Constants;
using Signify.FOBT.Svc.Core.Infrastructure.Observability;
using Fobt = Signify.FOBT.Svc.Core.Data.Entities.FOBT;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace FobtNsbEvents;

[ExcludeFromCodeCoverage]
public class FobtEvalReceived : ICommand
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

public class FobtEvalReceivedEventHandler : IHandleMessages<FobtEvalReceived>
{
    private readonly ILogger<FobtEvalReceivedEventHandler> _logger;
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;
    private readonly IPublishObservability _publishObservability;
        
    public FobtEvalReceivedEventHandler
    (
        ILogger<FobtEvalReceivedEventHandler> logger,
        IMapper mapper,
        IMediator mediator,
        IPublishObservability publishObservability
    )
    {
        _logger = logger;
        _mapper = mapper;
        _mediator = mediator;
        _publishObservability = publishObservability;
    }

    [Transaction]
    public async Task Handle(FobtEvalReceived fobtEvalReceived, IMessageHandlerContext context)
    {
        // Query database to check if FOBT exists.
        var fobt = await _mediator.Send(new GetFOBT { EvaluationId = fobtEvalReceived.EvaluationId }, context.CancellationToken);
        if (fobt != null)
        {
            await UpdateDateOfServiceAsync(fobtEvalReceived, fobt, context);
                
            PublishObservability(fobtEvalReceived, Observability.Evaluation.EvaluationClarificationEvent);
            return;
        }

        // Query Evaluation api and filter eval answers
        var barcode = await _mediator.Send(new CheckFOBTEval { EvaluationId = fobtEvalReceived.EvaluationId }, context.CancellationToken);

        var notPerformed = string.IsNullOrEmpty(barcode);
        if (notPerformed)
        {
            _logger.LogInformation("FOBT evaluation was not performed. The Fobt either was not delivered or no barcode found. EvaluationId:{EvaluationId}, EventId:{EventId}",
                fobtEvalReceived.EvaluationId,
                fobtEvalReceived.Id);
        }

        // Raise FOBT Evaluation Received NService bus event
        var evalReceived = _mapper.Map<EvaluationReceived>(fobtEvalReceived);
        evalReceived.Barcode = barcode;
        evalReceived.Performed = !notPerformed;

        await context.Publish(evalReceived);
    }

    /// <summary>
    /// This method compares Date of Service and updates the product when different
    /// </summary>
    /// <param name="fobtEvalReceived">FobtEvalReceived from kafka EvaluationFinalized event</param>
    /// <param name="fobt">Fobt Product from database table</param>
    /// <param name="context">Message Handler Context used to send DateOfServiceUpdated event when applicable</param>
    /// <returns>Completed Task</returns>
    internal async Task UpdateDateOfServiceAsync(FobtEvalReceived fobtEvalReceived, Fobt fobt, IMessageHandlerContext context)
    {
        if (Nullable.Compare(fobtEvalReceived.DateOfService, fobt.DateOfService) == 0)
        {
            _logger.LogDebug("Already processed evaluation and no change in Date of Service. EvaluationId:{EvaluationId}, EventId:{EventId}",
                fobtEvalReceived.EvaluationId,
                fobtEvalReceived.Id);

            return;
        }

        if (fobtEvalReceived.DateOfService == null)
        {
            _logger.LogInformation("Evaluation exists, Date of Service is null so no action was taken, EvaluationId:{EvaluationId}, EventId:{EventId}",
                fobtEvalReceived.EvaluationId,
                fobtEvalReceived.Id);
        }
        else
        {
            var dosUpdate = new DateOfServiceUpdated(fobtEvalReceived.EvaluationId, fobtEvalReceived.DateOfService.Value);
            await context.Publish(dosUpdate);

            _logger.LogInformation("Evaluation exists and DateOfServiceUpdated event published, EvaluationId:{EvaluationId}, EventId:{EventId}",
                fobtEvalReceived.EvaluationId,
                fobtEvalReceived.Id);
        }
    }
        
    private void PublishObservability(FobtEvalReceived fobtEvalReceived, string eventType)
    {
        var observabilityClarificationEvent = new ObservabilityEvent
        {
            EvaluationId = fobtEvalReceived.EvaluationId,
            EventType = eventType,
            EventValue = new Dictionary<string, object>
            {
                { Observability.EventParams.EvaluationId, fobtEvalReceived.EvaluationId },
                { Observability.EventParams.CreatedDateTime, fobtEvalReceived.CreatedDateTime.ToUnixTimeSeconds() }
            }
        };

        _publishObservability.RegisterEvent(observabilityClarificationEvent, true);
    }
}