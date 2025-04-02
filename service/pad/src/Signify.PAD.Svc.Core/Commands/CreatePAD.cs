using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.PAD.Messages.Events;
using Signify.PAD.Svc.Core.Data;
using Signify.PAD.Svc.Core.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using Signify.PAD.Svc.Core.Constants;

namespace Signify.PAD.Svc.Core.Commands;

/// <summary>
/// This handles Evaluation Received Event and raise PAD Performed Event.
/// </summary>
[ExcludeFromCodeCoverage]
public class CreatePad : ICommand
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
    public DateTimeOffset ReceivedDateTime { get; set; }
    public DateTime? DateOfService { get; set; }
    public List<Product> Products { get; set; }
    public Location Location { get; set; }
}

public class CreatePadHandler : IHandleMessages<CreatePad>
{
    private readonly ILogger<CreatePadHandler> _logger;
    private readonly IMapper _mapper;
    private readonly PADDataContext _dataContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly IPublishObservability _publishObservability;
        
    public CreatePadHandler(
        ILogger<CreatePadHandler> logger, 
        IMapper mapper, 
        IServiceProvider serviceProvider, 
        PADDataContext dataContext,
        IPublishObservability publishObservability)
    {
        _logger = logger;
        _mapper = mapper;
        _serviceProvider = serviceProvider;
        _dataContext = dataContext;
        _publishObservability = publishObservability;
    }

    [Transaction]
    public async Task Handle(CreatePad message, IMessageHandlerContext context)
    {
        //Query database to check if PAD exists.
        using var scope = _serviceProvider.CreateScope();
        scope.ServiceProvider.GetRequiredService<PADDataContext>();
        var pad = await _dataContext.PAD.FirstOrDefaultAsync(s => s.EvaluationId == message.EvaluationId, context.CancellationToken);
        if (pad != null)
        {
            await UpdateDateOfServiceAsync(message.DateOfService, pad, message.EvaluationId, message.Id, context);
            return;
        }

        //Raise PAD Evaluation Received NService bus event
        var evalReceived = _mapper.Map<EvalReceived>(message);
        await context.Publish(evalReceived);
    }

    /// <summary>
    /// This method compares DOS and updated the product when different
    /// </summary>
    /// <param name="eventDos">DOS from kafka event</param>
    /// <param name="pad">Product from database</param>
    /// <param name="evaluationId">Evaluation Id from the event</param>
    /// <param name="eventId">Unique id of the event</param>
    /// <returns></returns>
    internal async Task UpdateDateOfServiceAsync(DateTime? eventDos, Data.Entities.PAD pad, int evaluationId, Guid eventId, IMessageHandlerContext messageHandlerContext)
    {
        if (Nullable.Compare(eventDos, pad.DateOfService) == 0)
        {
            _logger.LogDebug("Task Completed. Already processed Eval and no change in DOS. EvaluationID: {EvaluationId}, EventId: {EventId}", evaluationId, eventId);
            PublishObservability(evaluationId, pad.CreatedDateTime.ToUnixTimeSeconds(), Observability.Evaluation.EvaluationClarificationEvent, true);
        }
        else //save updated DOS
        {
            if (eventDos == null)
            {
                _logger.LogInformation(
                    $"Evaluation exists, DOS is null and no action taken, EvaluationID : {evaluationId}, EventId: {eventId}");
            }
            else
            {
                var dosUpdate = new DateOfServiceUpdated(evaluationId, eventDos.Value);
                await messageHandlerContext.Publish(dosUpdate).ConfigureAwait(false);
                _logger.LogInformation(
                    $"Evaluation exists and DosUpdate event published, EvaluationID : {evaluationId}, EventId: {eventId}");
            }
        }

        _ = Task.CompletedTask;
    }
        
    private void PublishObservability(int evaluationId, long createdDateTime, string eventType, bool sendImmediate = false)
    {
        var observabilityDosUpdatedEvent = new ObservabilityEvent
        {
            EvaluationId = evaluationId,
            EventType = eventType,
            EventValue = new Dictionary<string, object>
            {
                { Observability.EventParams.EvaluationId, evaluationId },
                { Observability.EventParams.CreatedDateTime, createdDateTime }
            }
        };

        _publishObservability.RegisterEvent(observabilityDosUpdatedEvent, sendImmediate);
    }
}