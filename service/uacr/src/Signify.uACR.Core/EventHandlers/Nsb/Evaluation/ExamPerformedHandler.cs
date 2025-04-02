using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.uACR.Core.Commands;
using Signify.uACR.Core.Constants;
using Signify.uACR.Core.Data.Entities;
using Signify.uACR.Core.Data;
using Signify.uACR.Core.FeatureFlagging;
using Signify.uACR.Core.Infrastructure.Vendor;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Signify.Dps.Observability.Library.Services;
using Signify.uACR.Core.Infrastructure;
using UacrNsbEvents;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace NsbEventHandlers;

public class ExamPerformedHandler(
    ILogger<ExamPerformedHandler> logger,
    ITransactionSupplier transactionSupplier,
    IMediator mediator,
    IPublishObservability publishObservability,
    IFeatureFlags featureFlags,
    IVendorDetermination vendorDetermination,
    IApplicationTime applicationTime)
    : BaseNsbHandler(logger, mediator, transactionSupplier, publishObservability, applicationTime), IHandleMessages<ExamPerformedEvent>
{
    [Transaction]
    public async Task Handle(ExamPerformedEvent message, IMessageHandlerContext context)
    {
        using var transaction = TransactionSupplier.BeginTransaction();

        // Save the new exam to db
        var exam = await Mediator.Send(new AddExam(message.Exam), context.CancellationToken);

        // Save the new barcode to db
        var barcode = await Mediator.Send(new AddBarcode(message.Exam, message.Result), context.CancellationToken);

        await SendStatus(context, message, exam, barcode.Barcode);

        if (featureFlags.EnableOrderCreation)
        {
            //Log status of OrderCreation flag - this flag will be used for KED
            Logger.LogInformation(
                "Order creation is true, for EvaluationId={EvaluationId}, ExamId={ExamId}",
                exam.EvaluationId, exam.ExamId);

            var vendor = vendorDetermination.GetVendor(barcode.Barcode);
            // Is vendor in barcode
            if (VendorDetermination.Vendor.LetsGetChecked == vendorDetermination.GetVendor(barcode.Barcode) && message.Result.ValidBarcode)
            {
                var barcodeArray = SplitBarcode(barcode.Barcode);
                //send NSB OrderCreationEvent
                await context.SendLocal(new OrderCreationEvent()
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
                            barcodeArray is {Length: > 0} ? barcodeArray[0] : null
                        },
                        {
                            Vendor.LgcAlphaCode,
                            barcodeArray is {Length: 2} ? barcodeArray[1] : null
                        }
                    }
                });
            }
            else
            {
                PublishObservabilityEvents(exam.EvaluationId, exam.EvaluationReceivedDateTime,
                    Observability.OmsOrderCreation.OrderNotRequested,
                    new Dictionary<string, object>()
                    {
                        {Observability.EventParams.Barcode, barcode.Barcode},
                        {Observability.EventParams.Vendor, vendor.ToString()}
                    }, true);
            }
        }

        await transaction.CommitAsync(context.CancellationToken);

        Logger.LogInformation(
            "Finished handling evaluation where an exam was performed, with Barcode={Barcode}, EventId={EventId}, EvaluationId={EvaluationId}",
            barcode.Barcode, message.EventId, message.Exam?.EvaluationId);

        PublishObservabilityEvents(exam.EvaluationId, exam.EvaluationReceivedDateTime,
            Observability.Evaluation.EvaluationPerformedEvent, null, true);
    }

    private static string[] SplitBarcode(string barcode)
        => null == barcode ? [] : Array.ConvertAll(barcode.Split('|'), p => p.Trim());
    
    private static async Task SendStatus(IPipelineContext context, ExamPerformedEvent message, Exam exam, string barcode)
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