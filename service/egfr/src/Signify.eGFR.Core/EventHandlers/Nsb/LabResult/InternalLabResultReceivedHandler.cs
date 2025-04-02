using System;
using System.Collections.Generic;
using System.Net;
using AutoMapper;
using Hl7.Fhir.ElementModel;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.Dps.Observability.Library.Services;
using Signify.eGFR.Core.ApiClients.InternalLabResultApi;
using Signify.eGFR.Core.Constants;
using Signify.eGFR.Core.Data;
using Signify.eGFR.Core.Events;
using Signify.eGFR.Core.Exceptions;
using Signify.eGFR.Core.Infrastructure;
using Task = System.Threading.Tasks.Task;
using MediatR;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace NsbEventHandlers;

public class InternalLabResultReceivedHandler(
    ILogger<InternalLabResultReceivedHandler> logger,
    IMediator mediator,
    ITransactionSupplier transactionSupplier,
    IPublishObservability publishObservability,
    IApplicationTime applicationTime,
    IMapper mapper,
    IInternalLabResultApi internalLabResultApi)
    : BaseNsbHandler(logger, mediator, transactionSupplier, publishObservability, applicationTime), IHandleMessages<LabResultReceivedEvent>
{
    [Transaction]
    public async Task Handle(LabResultReceivedEvent @event, IMessageHandlerContext context)
    {
        Logger.LogInformation("Start handling InternalLabResult Event with LabResultId: {LabResultId}", @event.LabResultId);

        var response = await internalLabResultApi.GetLabResultByLabResultId(@event.LabResultId.ToString());

        var eventId = response.Content?.RequestId ?? Guid.NewGuid();

        PublishObservabilityEvents(eventId, ApplicationTime.UtcNow(),
            Observability.RmsIlrApi.GetLabResultByLabResultIdEvents,
            new Dictionary<string, object>
            {
                {Observability.EventParams.LabResultId, @event.LabResultId},
                {Observability.EventParams.Vendor, @event.VendorName},
                {Observability.EventParams.TestNames, string.Join(",", @event.ProductCodes)},
                {Observability.EventParams.StatusCode, response.StatusCode}
            }, true);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            Logger.LogError(
                "Failed to get lab result for {Vendor} with LabResultId: {LabResultId}. StatusCode: {StatusCode}",
                @event.VendorName, @event.LabResultId, response.StatusCode);

            throw new GetResultResponseUnsuccessfulException(@event.LabResultId, @event.VendorName,
                string.Join(",", @event.ProductCodes), response.StatusCode);
        }

        try
        {
            var labResult = mapper.Map<KedEgfrLabResult>(response.Content?.VendorData);
            labResult.ReceivedByEgfrDateTime = @event.ReceivedDateTime;
            
            PublishObservabilityEvents(eventId, ApplicationTime.UtcNow(),
                Observability.RmsIlrApi.LabResultMappedEvents,
                new Dictionary<string, object>
                {
                    { Observability.EventParams.LabResultId, @event.LabResultId },
                    { Observability.EventParams.Vendor, @event.VendorName },
                    { Observability.EventParams.TestNames, string.Join(",", @event.ProductCodes) },
                    { Observability.EventParams.StatusCode, response.StatusCode },
                    { Observability.EventParams.EvaluationId, labResult.EvaluationId }
                }, true);

            await context.SendLocal(labResult);
            Logger.LogInformation("Finished handling InternalLabResult Event with LabResultId={LabResultId}, EventId={EventId} EvaluationId={EvaluationId}",
                @event.LabResultId, eventId, labResult.EvaluationId);
        }
        catch (StructuralTypeException e)
        {
            Logger.LogError(e,
                "{LabResultReceivedEvent} StructuralTypeException Lab result FhirValidation failed for ILabResultId={LabResultId}, VendorName={VendorName}",
                nameof(LabResultReceivedEvent), @event.LabResultId, @event.VendorName);
            throw new FhirParseException("Fhir validation failed", null, @event.VendorName, @event.LabResultId, e);
        }
        catch (FormatException e)
        {
            Logger.LogError(e,
                "{LabResultReceivedEvent} FormatException Lab result FhirValidation failed for ILabResultId={LabResultId}, VendorName={VendorName}",
                nameof(LabResultReceivedEvent), @event.LabResultId, @event.VendorName);
            throw new FhirParseException("Fhir validation failed", null, @event.VendorName, @event.LabResultId, e);
        }
        catch (FhirParseException e) when (e is FhirParsePatientException ||
                                           e is FhirParseDiagnosticReportException ||
                                           e is FhirParseObservationException)
        {
            Logger.LogError(e,
                "{LabResultReceivedEvent} TypeofException={ExceptionType}, Lab result FhirValidation failed Exception for ILabResultId={LabResultId}, VendorName={VendorName}, EvaluationId={EvaluationId}",
                nameof(LabResultReceivedEvent), e.GetType(), @event.LabResultId, @event.VendorName, e.EvaluationId);

            throw new FhirParseException(
                "Error mapping FHIR object to KedUacrLabResult due to " + e.GetType(), e.EvaluationId,
                @event.VendorName, @event.LabResultId, e);
        }
        catch (Exception e)
        {
            Logger.LogError(e,
                "{LabResultReceivedEvent} Lab result FhirValidation failed Exception for ILabResultId={LabResultId}, VendorName={VendorName}",
                nameof(LabResultReceivedEvent), @event.LabResultId, @event.VendorName);
            throw new FhirParseException(
                "Error mapping FHIR object to KedUacrLabResult", null, @event.VendorName, @event.LabResultId, e);
        }
    }
}