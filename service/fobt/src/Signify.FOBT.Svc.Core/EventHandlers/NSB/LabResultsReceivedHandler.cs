using AutoMapper;
using FobtNsbEvents;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.FOBT.Messages.Events.Status;
using Signify.FOBT.Svc.Core.Commands;
using Signify.FOBT.Svc.Core.Constants;
using Signify.FOBT.Svc.Core.Data;
using Signify.FOBT.Svc.Core.Data.Entities;
using Signify.FOBT.Svc.Core.Events;
using Signify.FOBT.Svc.Core.Exceptions;
using Signify.FOBT.Svc.Core.Messages.Queries;
using Signify.FOBT.Svc.Core.Queries;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Signify.FOBT.Svc.Core.BusinessRules;
using Signify.FOBT.Svc.Core.Models;
using Signify.FOBT.Svc.Core.FeatureFlagging;
using Fobt = Signify.FOBT.Svc.Core.Data.Entities.FOBT;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace NsbEventHandlers;

public class LabResultsReceivedHandler : IHandleMessages<HomeAccessResultsReceived>
{
    private readonly ILogger _logger;
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly ITransactionSupplier _transactionSupplier;
    private readonly IFeatureFlags _featureFlags;
    private readonly IBillableRules _billableRules;

    public LabResultsReceivedHandler(ILogger<LabResultsReceivedHandler> logger,
        IMediator mediator,
        IMapper mapper,
        ITransactionSupplier transactionSupplier,
        IFeatureFlags featureFlags,
        IBillableRules billableRules)
    {
        _logger = logger;
        _mediator = mediator;
        _mapper = mapper;
        _transactionSupplier = transactionSupplier;
        _featureFlags = featureFlags;
        _billableRules = billableRules;
    }

    [Transaction]
    public async Task Handle(HomeAccessResultsReceived message, IMessageHandlerContext context)
    {
        var fobt = await GetCorrespondingFobt(message, context);

        using var _ = _logger.BeginScope("EventId={EventId}, EvaluationId={EvaluationId}, Barcode={Barcode}, OrderCorrelationId={OrderCorrelationId}",
            message.EventId, fobt.EvaluationId, message.Barcode, message.OrderCorrelationId);

        var labResult = await _mediator.Send(new GetLabResult { OrderCorrelationId = message.OrderCorrelationId }, context.CancellationToken);
        if (labResult != null)
        {
            _logger.LogInformation("Ignoring event, a record for this event already exists in the LabResults table");
            return;
        }

        _logger.LogInformation("Associating this Results event with EvaluationId {EvaluationId}", fobt.EvaluationId);

        using var transaction = _transactionSupplier.BeginTransaction();

        // A record will only be inserted if either no record exists for this FOBTId, or if
        // the first record found has IsNotNullOrWhiteSpace(Exception). If a record is not
        // inserted, this will be `null`.
        var labResultEntity = await InsertLabResults(fobt, message);
        if (labResultEntity == null)
        {
            // ANC-2776 - Multiple invalid results received for an evaluation are not tracked
            _logger.LogInformation("Nothing to do. Valid lab results were received previously");
            return;
        }

        if (IsLabResultValid(labResultEntity))
        {
            await ProcessValidResults(labResultEntity, fobt, message, context);
        }
        else
        {
            // It will only get in this section for the first kit that has invalid results.
            // ANC-2776 - Don't we want to track in db and publish the fact that invalid lab
            // results were received for all kits, and not just the first one?
            await ProcessInvalidResults(labResultEntity, fobt);
        }

        await transaction.CommitAsync(context.CancellationToken);

        _logger.LogInformation("Finished processing HomeAccessResultsReceived event");
    }

    private async Task<Fobt> GetCorrespondingFobt(HomeAccessResultsReceived message, IMessageHandlerContext context)
    {
        var fobt = await _mediator.Send(new GetFobtByOrderCorrelationId
        {
            OrderCorrelationId = message.OrderCorrelationId
        }, cancellationToken: context.CancellationToken);

        if (fobt != null)
            return fobt;

        _logger.LogWarning("Unable to match event to an FOBT record by OrderCorrelationId {OrderCorrelationId}, with Barcode {Barcode}, EventId {EventId}",
            message.OrderCorrelationId, message.Barcode, message.EventId);

        // ANC-4387 - Added Additional FOBT record lookup logic.  If we are unable to find the 
        // FOBT record by the provided Order Correlation Id, we will see if we can find a
        // record in Fobt Bardcode History table by the provided Barcode and Order
        // Correlation Id values.  If so, we will use that records FOBT Id to lookup the FOBT
        // record and return that value if it is found.
        var fobtByHistoryLookup = await _mediator.Send(new GetFobtByHistory 
        { 
            Barcode = message.Barcode, 
            OrderCorrelationId = message.OrderCorrelationId 
        }, cancellationToken: context.CancellationToken);

        if (fobtByHistoryLookup != null)
            return fobtByHistoryLookup;

        _logger.LogWarning("Unable to match event to an FOBT record looking up by history with OrderCorrelationId {OrderCorrelationId} and Barcode {Barcode}, EventId {EventId}",
            message.OrderCorrelationId, message.Barcode, message.EventId);

        throw new UnableToFindFobtException(message.OrderCorrelationId, message.Barcode);
    }

    private Task<LabResults> InsertLabResults(Fobt fobt, HomeAccessResultsReceived message)
    {
        var command = _mapper.Map<CreateLabResult>(message);
        command.FOBTId = fobt.FOBTId;
        return _mediator.Send(command);
    }

    private async Task ProcessValidResults(LabResults labResult, FOBT fobt, HomeAccessResultsReceived message, IMessageHandlerContext context)
    {
        Task CreateStatus(FOBTStatusCode statusCode)
        {
            return _mediator.Send(new CreateFOBTStatus
            {
                FOBT = fobt,
                StatusCode = statusCode
            }, context.CancellationToken);
        }

        _logger.LogInformation("Valid lab results received");

        await CreateStatus(FOBTStatusCode.ValidLabResultsReceived);

        var isBillableForResults = IsBillable(fobt, labResult);
        var pdf = await _mediator.Send(new GetPDFToClient(fobt.FOBTId, fobt.EvaluationId!.Value), context.CancellationToken);

        if (pdf is not null)
        {
            if (isBillableForResults)
            {
                _logger.LogInformation("Evaluation is billable and pdf exists. Sending a BillRequest");
                await SendBillingRequest(context, pdf, fobt);
            }
            else
            {
                _logger.LogInformation("Evaluation is not billable and pdf exists. Publishing BillRequestNotSend");

                await CallBillRequestNotSentPublisher(fobt);
                await CreateStatus(FOBTStatusCode.BillRequestNotSent);
            }
        }

        await PublishResults(fobt, labResult, isBillableForResults);
        await ProcessPayment(message, fobt, context);
    }

    private async Task ProcessInvalidResults(LabResults labResult, Fobt fobt)
    {
        Task CreateStatus(FOBTStatusCode statusCode)
        {
            return _mediator.Send(new CreateFOBTStatus
            {
                FOBT = fobt,
                StatusCode = statusCode
            });
        }

        _logger.LogInformation("Invalid lab results received");

        await CreateStatus(FOBTStatusCode.InvalidLabResultsReceived);

        await PublishResults(fobt, labResult, false);

        //Publish bill request not sent event and add status
        await CallBillRequestNotSentPublisher(fobt);
        await CreateStatus(FOBTStatusCode.BillRequestNotSent);
    }

    private Task PublishResults(FOBT fobt, LabResults result, bool isBillableFlag)
        => _mediator.Send(new PublishResults(fobt, result, isBillableFlag));

    private async Task SendBillingRequest(IMessageHandlerContext context, PDFToClient eventMessage, FOBT fobt)
    {
        const string billingProductCode = ApplicationConstants.BILLING_PRODUCT_CODE_RESULTS;

        var rcmBillingRequest = _mapper.Map<RCMRequestEvent>(fobt);
        rcmBillingRequest.ApplicationId = ApplicationConstants.BILLING_APPLICATION_ID;
        rcmBillingRequest.RcmProductCode = billingProductCode;
        rcmBillingRequest.BillingProductCode = billingProductCode;
        rcmBillingRequest.BillableDate = eventMessage.DeliveryDateTime;
        rcmBillingRequest.SharedClientId = fobt.ClientId!.Value;
        rcmBillingRequest.CorrelationId = Guid.NewGuid().ToString();
        rcmBillingRequest.StatusCode = FOBTStatusCode.ResultsBillRequestSent.StatusCode;
        rcmBillingRequest.FOBT = fobt;
        rcmBillingRequest.AdditionalDetails = new Dictionary<string, string>
        {
            { "BatchName", eventMessage.BatchName },
            { "EvaluationId", rcmBillingRequest.EvaluationId.ToString() },
            { "appointmentId",  fobt.AppointmentId.ToString() }
        };

        await context.Publish(rcmBillingRequest);

        _logger.LogInformation("Billing request for RcmProductCode {RcmProductCode} enqueued for processing, with CorrelationId {CorrelationId}",
            billingProductCode, rcmBillingRequest.CorrelationId);
    }

    private async Task CallBillRequestNotSentPublisher(FOBT fobt)
    {
        var publishBillRequestNotSent = _mapper.Map<BillRequestNotSent>(fobt);
        publishBillRequestNotSent.ReceivedDate = DateTime.UtcNow;
        await _mediator.Send(new PublishStatusUpdate(publishBillRequestNotSent));
    }

    /// <summary>
    /// Handle Provider Payment
    /// </summary>
    /// <param name="message"></param>
    /// <param name="exam"></param>
    /// <param name="context"></param>
    private async Task ProcessPayment(HomeAccessResultsReceived message, FOBT exam, IMessageHandlerContext context)
    {
        var examStatus = await GetLatestCdiEvent(exam.EvaluationId);
        if (examStatus?.FOBTStatusCodeId is (int)FOBTStatusCode.StatusCodes.CdiPassedReceived or (int)FOBTStatusCode.StatusCodes.CdiFailedWithPayReceived)
        {
            await SendProviderPayRequest(examStatus, message, exam, context);
        }
        else
        {
            _logger.LogInformation("A valid CDI event has not been received for EvaluationId={EvaluationId}. ProviderPay will not be triggered",
                exam.EvaluationId);
        }
    }


    private bool IsBillable(Fobt fobt, LabResults labResult)
    {
        return _billableRules.IsBillableForResults(new BillableRuleAnswers
        {
            Exam = fobt,
            LabResults = labResult
        }).IsMet;
    }

    /// <summary>
    /// Check if a lab result is valid
    /// </summary>
    /// <param name="labResult"></param>
    /// <returns></returns>
    private bool IsLabResultValid(LabResults labResult)
    {
        var answers = new BillableRuleAnswers
        {
            LabResults = labResult
        };
        return _billableRules.IsLabResultValid(answers).IsMet;
    }

    /// <summary>
    /// Get the latest cdi_events event received by FOBT 
    /// </summary>
    /// <param name="evaluationId"></param>
    /// <returns></returns>
    private async Task<FOBTStatus> GetLatestCdiEvent(int? evaluationId)
    {
        var examStatus = await _mediator.Send(new GetLatestCdiEvent { EvaluationId = evaluationId!.Value });

        return examStatus;
    }

    /// <summary>
    /// Send NSB event to handle database write and kafka event
    /// </summary>
    /// <param name="examStatus"></param>
    /// <param name="message"></param>
    /// <param name="exam"></param>
    /// <param name="context"></param>
    [Trace]
    private async Task SendProviderPayRequest(FOBTStatus examStatus, HomeAccessResultsReceived message, FOBT exam,
        IPipelineContext context)
    {
        var providerPayEventRequest = _mapper.Map<ProviderPayRequest>(exam);
        providerPayEventRequest.PersonId = exam.CenseoId;
        providerPayEventRequest.EventId = message.EventId;
        providerPayEventRequest.ParentEventDateTime = examStatus.CreatedDateTime;
        providerPayEventRequest.ParentEventReceivedDateTime = examStatus.CreatedDateTime;
        providerPayEventRequest.ParentEvent =
            examStatus.FOBTStatusCodeId == FOBTStatusCode.CdiPassedReceived.FOBTStatusCodeId
                ? nameof(CDIPassedEvent)
                : nameof(CDIFailedEvent);

        providerPayEventRequest.AdditionalDetails = new Dictionary<string, string>
        {
            { "EvaluationId", exam.EvaluationId.ToString() },
            { "AppointmentId", exam.AppointmentId.ToString() },
            { "ExamId", exam.FOBTId.ToString() }
        };

        await context.SendLocal(providerPayEventRequest);
    }
}