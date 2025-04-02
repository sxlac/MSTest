using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Signify.Spirometry.Core.Sagas.Models;
using SpiroNsb.SagaCommands;
using SpiroNsb.SagaEvents;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace SpiroNsb.Sagas;

public class ProviderPaySaga :
    //Saga Data
    Saga<ProviderPaySagaData>,
    // Events that can start a Saga i.e. set Saga Data
    IAmStartedByMessages<EvaluationProcessedEventForPayment>,
    IAmStartedByMessages<OverreadProcessedEventForPayment>,
    IAmStartedByMessages<CdiEventForPaymentReceived>,
    // Events that are to be handled by this Saga but cannot start a Saga
    IHandleMessages<ProviderPaidEvent>

{
    private readonly ILogger<ProviderPaySaga> _logger;

    public ProviderPaySaga(ILogger<ProviderPaySaga> logger)
    {
        _logger = logger;
    }

    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<ProviderPaySagaData> mapper)
    {
        mapper.MapSaga(saga => saga.EvaluationId)
            .ToMessage<CdiEventForPaymentReceived>(evt => evt.EvaluationId)
            .ToMessage<OverreadProcessedEventForPayment>(evt => evt.EvaluationId)
            .ToMessage<EvaluationProcessedEventForPayment>(evt => evt.EvaluationId)
            .ToMessage<ProviderPaidEvent>(evt => evt.EvaluationId);
    }

    public async Task Handle(EvaluationProcessedEventForPayment message, IMessageHandlerContext context)
    {
        _logger.LogInformation("Received {EventType} for EvaluationId={EvaluationId}",
            nameof(EvaluationProcessedEventForPayment), message.EvaluationId);

        Data.IsPerformed = message.IsPerformed;
        Data.SpirometryExamId = message.SpirometryExamId;
        Data.FinalizedProcessedDateTime = message.CreatedDateTime;

        if (!message.IsPerformed)
        {
            // Although the handler that raised this event should have set the NeedsOverread property on the
            // message correctly, we know these should always be false when NotPerformed, so just hard-code it
            Data.NeedsOverread = Data.IsPayable = false;
        }
        else
        {
            // We always know whether we need an overread at time of POC (A/B/C vs D/E/F)
            Data.NeedsOverread = message.NeedsOverread;

            // Data.IsPayable is definitely `null` before receiving this event, because it's the first event
            // that can possibly be received that has an IsPayable property (Finalized and OverreadProcessed
            // (which is triggered by this saga only once a Finalized is received) are the only events with a
            // IsPayable flag).
            Data.IsPayable = message.IsPayable;
        }

        await EnqueueOutstandingTasks(context);
    }

    public async Task Handle(OverreadProcessedEventForPayment message, IMessageHandlerContext context)
    {
        _logger.LogInformation("Received {EventType} for EvaluationId={EvaluationId}",
            nameof(OverreadProcessedEventForPayment), message.EvaluationId);
        Data.OverreadProcessedDateTime = message.CreatedDateTime;
        Data.IsPayable = message.IsPayable;
        await EnqueueOutstandingTasks(context);
    }

    public async Task Handle(CdiEventForPaymentReceived message, IMessageHandlerContext context)
    {
        _logger.LogInformation("Received {EventType} for EvaluationId={EvaluationId}",
            nameof(CdiEventForPaymentReceived), message.EvaluationId);
        Data.CdiEventReceivedDateTime = message.CreatedDateTime;

        await EnqueueOutstandingTasks(context);
    }

    public Task Handle(ProviderPaidEvent message, IMessageHandlerContext context)
    {
        _logger.LogInformation("Marking saga for EvaluationId={EvaluationId} as complete", Data.EvaluationId);
        Data.IsPaymentComplete = true;
        MarkAsComplete();
        return Task.CompletedTask;
    }

    #region Rules

    /// <summary>
    /// This becomes true when an EvaluationFinalized event has been handled
    /// </summary>
    private bool HasProcessedFinalized
    {
        get
        {
            if (Data.IsPerformed.HasValue && !Data.IsPerformed.Value)
            {
                _logger.LogInformation("Nothing to do, exam was not performed, for EvaluationId={EvaluationId}", Data.EvaluationId);
            }

            return Data.IsPerformed.HasValue;
        }
    }

    private bool ShouldProcessPayment => HasProcessedFinalized // ExamId is available
                                         && Data.IsPerformed!.Value
                                         && Data.NeedsOverread.HasValue
                                         && (!Data.NeedsOverread.Value || Data.OverreadProcessedDateTime.HasValue) // ABC, or DEF and overread processed
                                         && !Data.IsPaymentComplete // payment has not been done yet via another cdi event
                                         && Data.IsPayable.HasValue // able to determine ability to pay
                                         && Data.CdiEventReceivedDateTime.HasValue; // indicates that a cdi event has already arrived

    #endregion

    #region Tasks

    private Task EnqueueOutstandingTasks(IPipelineContext context)
    {
        var tasks = new List<Task>();

        if (ShouldProcessPayment)
            tasks.Add(ProcessPayment(context));

        if (!tasks.Any())
            _logger.LogInformation("Nothing to do for EvaluationId={EvaluationId} at this time", Data.EvaluationId);
        else
            _logger.LogInformation("Enqueuing {TaskCount} outstanding tasks for EvaluationId={EvaluationId}", tasks.Count, Data.EvaluationId);

        return Task.WhenAll(tasks);
    }

    private async Task ProcessPayment(IPipelineContext context)
    {
        await context.SendLocal(new ProcessProviderPay(Data.EvaluationId, Data.IsPayable!.Value));
    }

    #endregion
}