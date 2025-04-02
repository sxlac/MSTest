using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.DEE.Svc.Core.ApiClients.CdiApi.Holds;
using Signify.DEE.Svc.Core.ApiClients.CdiApi.Holds.Requests;
using Signify.DEE.Svc.Core.Constants;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Exceptions;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.EventHandlers.Nsb
{
    [ExcludeFromCodeCoverage]
    public class ReleaseHold : ICommand
    {
        /// <summary>
        /// Identifier of this hold in the DEE database
        /// </summary>
        /// <remarks>
        /// Not to be mistaken for the hold's identifier outside of the DEE context, which
        /// is the CdiHoldId
        /// </remarks>
        public Hold Hold { get; set; }

        public ReleaseHold(Hold hold)
        {
            Hold = hold;
        }
    }

    public class ReleaseHoldHandler(
        ILogger<ReleaseHoldHandler> logger,
        ICdiHoldsApi cdiHoldsApi)
        : IHandleMessages<ReleaseHold>
    {

        [Transaction]
        public async Task Handle(ReleaseHold message, IMessageHandlerContext context)
        {
            logger.LogInformation("Received ReleaseHold request for EvaluationId={EvaluationId}, for HoldId={HoldId}", message.Hold.EvaluationId, message.Hold.HoldId);

            if (message.Hold.ReleasedDateTime.HasValue)
            {
                logger.LogInformation("HoldId={HoldId} for EvaluationId={EvaluationId} was already released at {ReleasedDateTime}, nothing left to do",
                    message.Hold.HoldId, message.Hold.EvaluationId, message.Hold.ReleasedDateTime);
                return;
            }

            await RequestHoldToBeReleased(message.Hold);

            // Don't raise `HoldReleasedEvent` here; let it get raised when we receive a released event from Kafka
        }

        private async Task RequestHoldToBeReleased(Hold hold)
        {
            // See: https://dev.azure.com/signifyhealth/HCC/_git/cdi?path=/legacy/api/cdiapi/src/CH.CDI.WebApi/Controllers/HoldsController.cs

            // Response codes returned by API:
            // Accepted (202)
            // Not Found (404)

            logger.LogInformation("Requesting to release CdiHoldId={CdiHoldId} (HoldId={HoldId}), for EvaluationId={EvaluationId}",
                hold.CdiHoldId, hold.HoldId, hold.EvaluationId);

            var response = await cdiHoldsApi.ReleaseHold(hold.CdiHoldId, new ReleaseHoldRequest
            {
                ApplicationId = ApplicationConstants.ServiceName,
                ProductCodes = [ApplicationConstants.ProductCode]
            });

            logger.Log(response.IsSuccessStatusCode ? LogLevel.Information : LogLevel.Warning,
                "Received StatusCode={StatusCode} from CDI Holds API, for CdiHoldId={CdiHoldId} (HoldId={HoldId}), for EvaluationId={EvaluationId}",
                response.StatusCode, hold.CdiHoldId, hold.HoldId, hold.EvaluationId);

            if (response.IsSuccessStatusCode)
                return;

            // Raise for NSB retry
            throw new ReleaseHoldRequestException(hold.EvaluationId, hold.HoldId, hold.CdiHoldId, response.Error);
        }
    }
}
