using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.Spirometry.Core.ApiClients.CdiApi.Holds;
using Signify.Spirometry.Core.ApiClients.CdiApi.Holds.Requests;
using Signify.Spirometry.Core.Constants;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Exceptions;
using Signify.Spirometry.Core.Queries;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace SpiroNsb.SagaCommands;

/// <summary>
/// Command to release an evaluation held in CDI
/// </summary>
public class ReleaseHold : ISagaCommand
{
    /// <inheritdoc />
    public long EvaluationId { get; set; }

    /// <summary>
    /// Identifier of this hold in the Spirometry database
    /// </summary>
    /// <remarks>
    /// Not to be mistaken for the hold's identifier outside of the Spirometry context, which
    /// is the CdiHoldId
    /// </remarks>
    public int HoldId { get; set; }

    public ReleaseHold(long evaluationId, int holdId)
    {
        EvaluationId = evaluationId;
        HoldId = holdId;
    }
}

public class ReleaseHoldHandler : IHandleMessages<ReleaseHold>
{
    private readonly ILogger _logger;
    private readonly ICdiHoldsApi _cdiHoldsApi;
    private readonly IMediator _mediator;

    public ReleaseHoldHandler(ILogger<ReleaseHoldHandler> logger,
        ICdiHoldsApi cdiHoldsApi,
        IMediator mediator)
    {
        _logger = logger;
        _cdiHoldsApi = cdiHoldsApi;
        _mediator = mediator;
    }

    [Transaction]
    public async Task Handle(ReleaseHold message, IMessageHandlerContext context)
    {
        _logger.LogInformation("Received ReleaseHold request for EvaluationId={EvaluationId}, for HoldId={HoldId}", message.EvaluationId, message.HoldId);

        var hold = await _mediator.Send(new QueryHold
        {
            EvaluationId = message.EvaluationId
        }, context.CancellationToken);

        if (hold.ReleasedDateTime.HasValue)
        {
            _logger.LogInformation("HoldId={HoldId} for EvaluationId={EvaluationId} was already released at {ReleasedDateTime}, nothing left to do",
                hold.HoldId, hold.EvaluationId, hold.ReleasedDateTime);
            return;
        }

        await RequestHoldToBeReleased(hold);

        // Don't raise `HoldReleasedEvent` here; let it get raised when we receive a released event from Kafka
    }

    private async Task RequestHoldToBeReleased(Hold hold)
    {
        // See: https://dev.azure.com/signifyhealth/HCC/_git/cdi?path=/legacy/api/cdiapi/src/CH.CDI.WebApi/Controllers/HoldsController.cs

        // Response codes returned by API:
        // Accepted (202)
        // Not Found (404)

        _logger.LogInformation("Requesting to release CdiHoldId={CdiHoldId} (HoldId={HoldId}), for EvaluationId={EvaluationId}", 
            hold.CdiHoldId, hold.HoldId, hold.EvaluationId);

        var response = await _cdiHoldsApi.ReleaseHold(hold.CdiHoldId, new ReleaseHoldRequest
        {
            ApplicationId = Application.ApplicationId,
            ProductCodes = new []
            {
                ProductCodes.Spirometry
            }
        });

        _logger.Log(response.IsSuccessStatusCode ? LogLevel.Information : LogLevel.Warning,
            "Received StatusCode={StatusCode} from CDI Holds API, for CdiHoldId={CdiHoldId} (HoldId={HoldId}), for EvaluationId={EvaluationId}",
            response.StatusCode, hold.CdiHoldId, hold.HoldId, hold.EvaluationId);

        if (response.IsSuccessStatusCode)
            return;

        // Raise for NSB retry
        throw new ReleaseHoldRequestException(hold.EvaluationId, hold.HoldId, hold.CdiHoldId, response.Error);
    }
}
