using NewRelic.Api.Agent;
using Signify.DEE.Svc.Core.Constants;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using System;
using System.Collections.Generic;
using MediatR;

using System.Threading.Tasks;
using System.Threading;

namespace Signify.DEE.Svc.Core.Messages.Commands
{
    // ATTN! If SendImmediately is not set, then somewhere in your flow there must be a call to publishObservability.Commit
    public class RegisterObservabilityEvent : IRequest
    {
        public long EvaluationId { get; set; }
        public string EventType { get; set; }
        public DateTimeOffset CreatedDateTime { get; set; } = DateTimeOffset.UtcNow;
        public bool SendImmediately { get; set; } = true;        
    }

    public class RegisterObservabilityEventHandler(
        IPublishObservability publishObservability) : IRequestHandler<RegisterObservabilityEvent>
    {
        [Transaction]
        public async Task Handle(RegisterObservabilityEvent evt, CancellationToken cancellationToken)
        {
            var observabilityEvent = new ObservabilityEvent
            {
                EvaluationId = evt.EvaluationId,
                EventType = evt.EventType,
                EventValue = new Dictionary<string, object>
                {
                    { Observability.EventParams.EvaluationId, evt.EvaluationId },
                    { Observability.EventParams.CreatedDateTime, evt.CreatedDateTime.ToUnixTimeSeconds() }
                }
            };

            publishObservability.RegisterEvent(observabilityEvent, evt.SendImmediately);
            await Task.CompletedTask;
        }
    }
}
