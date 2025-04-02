using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.AkkaStreams.Kafka.Persistence;
using Signify.eGFR.Core.Constants;
using Signify.eGFR.Core.Data;
using Signify.eGFR.Core.Infrastructure;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace NsbEventHandlers;

/// <summary>
/// Abstract class containing the most common Dependency Injections for NServiceBus Handlers
/// </summary>
public abstract class BaseNsbHandler(
    ILogger logger,
    IMediator mediator,
    ITransactionSupplier transactionSupplier,
    IPublishObservability publishObservability,
    IApplicationTime applicationTime)
{
    protected readonly ILogger Logger = logger;
    protected readonly IMediator Mediator = mediator;
    protected readonly ITransactionSupplier TransactionSupplier = transactionSupplier;
    protected readonly IApplicationTime ApplicationTime = applicationTime;

    /// <summary>
    /// Commit both TransactionSupplier and Observability transactions
    /// </summary>
    /// <param name="transaction">The transaction generated as part of TransactionSupplier.BeginTransaction</param>
    /// <param name="cancellationToken"></param>
    [Transaction]
    protected async Task CommitTransactions(IBufferedTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        await transaction.CommitAsync(cancellationToken);
        publishObservability.Commit();
    }

    #region Observability

    /// <summary>
    /// Publish to Observability platform
    /// </summary>
    /// <param name="evaluationId">evaluationId of the exam</param>
    /// <param name="eventCreatedDateTime">datetime the event was created in UTC</param>
    /// <param name="eventType">type of observability event</param>
    /// <param name="additionalDetails"></param>
    /// <param name="sendImmediate">whether to publish event immediately or wait for a commit command</param>
    [Trace]
    protected void PublishObservabilityEvents(long evaluationId, DateTimeOffset eventCreatedDateTime, string eventType,
        Dictionary<string, object> additionalDetails = null,
        bool sendImmediate = false)
    {
        try
        {
            var observabilityEvent = new ObservabilityEvent
            {
                EvaluationId = evaluationId,
                EventType = eventType,
                EventValue = new Dictionary<string, object>
                {
                    { Observability.EventParams.EvaluationId, evaluationId },
                    { Observability.EventParams.CreatedDateTime, eventCreatedDateTime.ToUnixTimeSeconds() }
                }
            };
            if (additionalDetails?.Count > 0)
            {
                foreach (var (key, value) in additionalDetails)
                {
                    observabilityEvent.EventValue.Add(key, value);
                }
            }

            publishObservability.RegisterEvent(observabilityEvent, sendImmediate);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex,
                "Exception while trying to add observability for EvaluationId={EvaluationId}",
                evaluationId);
        }
    }

    /// <summary>
    /// Publish to Observability platform
    /// </summary>
    /// <param name="eventId">id of the event</param>
    /// <param name="eventCreatedDateTime">datetime the event was created in UTC</param>
    /// <param name="eventType">type of observability event</param>
    /// <param name="additionalDetails"></param>
    /// <param name="sendImmediate">whether to publish event immediately or wait for a commit command</param>
    [Trace]
    protected void PublishObservabilityEvents(Guid? eventId, DateTimeOffset eventCreatedDateTime, string eventType,
        Dictionary<string, object> additionalDetails = null,
        bool sendImmediate = false)
    {
        try
        {
            var observabilityEvent = new ObservabilityEvent
            {
                EventId = eventId,
                EventType = eventType,
                EventValue = new Dictionary<string, object>
                {
                    { Observability.EventParams.EventId, eventId },
                    { Observability.EventParams.CreatedDateTime, eventCreatedDateTime.ToUnixTimeSeconds() }
                }
            };
            if (additionalDetails?.Count > 0)
            {
                foreach (var (key, value) in additionalDetails)
                {
                    observabilityEvent.EventValue.Add(key, value);
                }
            }

            publishObservability.RegisterEvent(observabilityEvent, sendImmediate);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex,
                "Exception while trying to add observability for EventId={EventId}",
                eventId);
        }
    }

    #endregion Observability
}