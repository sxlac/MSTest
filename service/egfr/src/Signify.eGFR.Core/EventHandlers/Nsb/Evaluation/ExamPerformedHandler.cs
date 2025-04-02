using EgfrNsbEvents;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.eGFR.Core.Commands;
using Signify.eGFR.Core.Constants;
using Signify.eGFR.Core.Data.Entities;
using Signify.eGFR.Core.Data;
using Signify.eGFR.Core.FeatureFlagging;
using Signify.eGFR.Core.Infrastructure.Vendor;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Signify.Dps.Observability.Library.Services;
using Signify.eGFR.Core.Infrastructure;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named. 
namespace NsbEventHandlers;

public class ExamPerformedHandler(
    ILogger<ExamPerformedHandler> logger,
    IMediator mediator,
    ITransactionSupplier transactionSupplier,
    IPublishObservability publishObservability,
    IApplicationTime applicationTime,
    IFeatureFlags featureFlags,
    IVendorDetermination vendorDetermination)
    : BaseNsbHandler(logger, mediator, transactionSupplier, publishObservability, applicationTime), IHandleMessages<ExamPerformedEvent>
{
    [Transaction]
    public async Task Handle(ExamPerformedEvent message, IMessageHandlerContext context)
    {
        Logger.LogDebug(
            "Started handling evaluation where an exam was performed, with EventId={EventId}, EvaluationId={EvaluationId}",
            message.EventId, message.Exam?.EvaluationId);

        using var transaction = TransactionSupplier.BeginTransaction();

        // Save the new exam to db
        var exam = await Mediator.Send(new AddExam(message.Exam), context.CancellationToken);

        // Save the new barcode to db
        var barcode = await Mediator.Send(new AddBarcode(message.Exam, message.Result), context.CancellationToken);

        //TODO: Add error handling here to handle an edge case:
        // If insert fails due to EvaluationId already exists in db, then this event is being processed from
        // an NSB error queue. After it was placed on the error queue, and before now, we received another
        // EvaluationFinalized event for this evaluation that was in fact an update (ex date of service was updated),
        // but that event was treated as the first time we've seen this evaluation since it wasn't yet in our db,
        // so it was inserted before this current event was finished processing from the error queue.
        await SendStatus(context, message, exam, barcode.Barcode);

        if (featureFlags.EnableOrderCreation)
        {
            //Log status of OrderCreation flag - this flag will be used for KED
            Logger.LogInformation(
                "Order creation is true, for EvaluationId={EvaluationId}, ExamId={ExamId}",
                exam.EvaluationId, exam.ExamId);

            var vendor = vendorDetermination.GetVendorFromBarcode(barcode.Barcode);
            // Is vendor in barcode
            if (VendorDetermination.Vendor.LetsGetChecked == vendorDetermination.GetVendorFromBarcode(barcode.Barcode) && message.Result.ValidBarcode)
            {
                await SendOrderCreationEventForLgc(message, context, barcode, exam, vendor);
            }
            else
            {
                PublishObservabilityEvents(exam.EvaluationId, exam.CreatedDateTime,
                    Observability.OmsOrderCreation.OrderNotRequested,
                    new Dictionary<string, object>
                    {
                        {Observability.EventParams.Vendor, vendor},
                        {Observability.EventParams.Barcode, barcode}
                    }, true);
            }
        }
        await transaction.CommitAsync(context.CancellationToken);

        Logger.LogInformation(
            "Finished handling evaluation where an exam was performed, with Barcode={Barcode}, EventId={EventId}, EvaluationId={EvaluationId}",
            barcode.Barcode, message.EventId, message.Exam?.EvaluationId);

        PublishObservabilityEvents(exam.EvaluationId, exam.CreatedDateTime, Observability.Evaluation.EvaluationPerformedEvent, null, true);
    }

    /// <summary>
    /// Send NSB OrderCreationEvent for Vendor LetsGetChecked with context containing LetsGetChecked barcode
    /// </summary>
    /// <param name="message"></param>
    /// <param name="context"></param>
    /// <param name="barcode"></param>
    /// <param name="exam"></param>
    /// <param name="vendor"></param>
    private static async Task SendOrderCreationEventForLgc(ExamPerformedEvent message, IMessageHandlerContext context,
        BarcodeHistory barcode, Exam exam, VendorDetermination.Vendor vendor)
    {
        var barcodeArray = SplitBarcode(barcode.Barcode);
        await context.SendLocal(new OrderCreationEvent
        {
            ExamId = exam.ExamId,
            StatusDateTime = exam.EvaluationReceivedDateTime,
            EventId = message.EventId,
            EvaluationId = exam.EvaluationId,
            Vendor = vendor.ToString(),
            Context = new Dictionary<string, string>
            {
                {
                    Vendor.LgcBarcode,
                    barcodeArray is { Length: > 0 } ? barcodeArray[0] : null
                },
                {
                    Vendor.LgcAlphaCode,
                    barcodeArray is { Length: 2 } ? barcodeArray[1] : null
                }
            }
        });
    }

    private static string[] SplitBarcode(string barcode) =>
        null == barcode ? [] : Array.ConvertAll(barcode.Split('|'), p => p.Trim());

    private static async Task SendStatus(IPipelineContext context, ExamPerformedEvent message, Exam exam,
        string barcode)
    {
        await context.SendLocal(new ExamStatusEvent
        {
            EventId = message.EventId,
            EvaluationId = exam.EvaluationId,
            ExamId = exam.ExamId,
            StatusCode = ExamStatusCode.ExamPerformed,
            StatusDateTime = message.Exam.EvaluationReceivedDateTime,
            Barcode = barcode
        });
    }
}