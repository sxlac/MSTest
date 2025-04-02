using AutoMapper;
using Iris.Public.Types.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.DEE.Messages.Status;
using Signify.DEE.Svc.Core.BusinessRules;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Events;
using Signify.DEE.Svc.Core.FeatureFlagging;
using Signify.DEE.Svc.Core.Infrastructure;
using Signify.DEE.Svc.Core.Messages.Commands;
using Signify.DEE.Svc.Core.Messages.Models;
using Signify.DEE.Svc.Core.Messages.Queries;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.Commands;

[ExcludeFromCodeCoverage]
public class DetermineBillabityOfResult : ICommand
{
    public ExamModel Exam { get; set; }
    public ResultImageDetails ImageDetails { get; set; }
    public ResultGrading Gradings { get; set; }
}

public class DetermineBillabityOfResultHandler(
    ILogger<DetermineBillabityOfResultHandler> logger,
    IMediator mediator,
    IMapper mapper,
    ITransactionSupplier transactionSupplier,
    IApplicationTime applicationTime,
    IBillableRules billableRules,
    IFeatureFlags featureFlags)
    : IHandleMessages<DetermineBillabityOfResult>
{
    [Transaction]
    public async Task Handle(DetermineBillabityOfResult command, IMessageHandlerContext context)
    {
        using var transaction = transactionSupplier.BeginTransaction();

        //Log the number of left images and right images found at IRIS end
        logger.LogInformation(
            "IRIS results indicate that EvaluationId: {EvaluationId} has {LeftEyeOriginalCount} left eye images and {RightEyeOriginalCount} right eye images",
            command.Exam.EvaluationId, command.ImageDetails.LeftEyeOriginalCount, command.ImageDetails.RightEyeOriginalCount);

        //Check for left and right image count. If one of them has value zero. Set Status Incomplete
        if ((command.ImageDetails.LeftEyeOriginalCount == 0 || command.ImageDetails.RightEyeOriginalCount == 0) && (!command.Exam.HasEnucleation.HasValue || !command.Exam.HasEnucleation.Value))
        {
            logger.LogInformation("ExamId: {ExamId} -- does not have enough images, updating status as Incomplete", command.Exam.ExamId);
            await mediator.Send(new CreateStatus(command.Exam.ExamId, ExamStatusCode.Incomplete.Name, applicationTime.UtcNow()), context.CancellationToken).ConfigureAwait(false);
        }

        // Create Gradable / Non Gradable status based on Eye Findings
        var gradable = billableRules.IsGradable(new BillableRuleAnswers { Gradings = command.Gradings });
        logger.LogInformation(
            gradable.IsMet
                ? "ExamId: {ExamId} -- has at least one finding, updating status as Gradable"
                : "ExamId: {ExamId} -- has no findings, updating status as Not Gradable",
            command.Exam.ExamId);

        //Update Status Gradable or not gradable.
        await mediator.Send(new CreateStatus(command.Exam.ExamId, gradable.IsMet ? ExamStatusCode.Gradable.Name : ExamStatusCode.NotGradable.Name,
            applicationTime.UtcNow()), context.CancellationToken).ConfigureAwait(false);

        //Update gradable flag in Exam table
        await mediator.Send(new UpdateExamGrade { ExamId = command.Exam.ExamId, Gradable = gradable.IsMet }, context.CancellationToken);

        await ProcessBilling(command, context);
        await ProcessPayment(command.Exam, context);
        await transaction.CommitAsync(context.CancellationToken);
    }

    /// <summary>
    /// Handle RCM Billing
    /// </summary>
    /// <param name="determineBillability"></param>
    /// <param name="context"></param>
    private async Task ProcessBilling(DetermineBillabityOfResult determineBillability, IMessageHandlerContext context)
    {
        var pdfDataToClient = await mediator.Send(new GetPdfToClient(determineBillability.Exam.ExamId, determineBillability.Exam.EvaluationId), context.CancellationToken);
        if (pdfDataToClient.EvaluationId == 0)
        {
            //Pdf Data is not available. Log something out here.
            logger.LogInformation("ExamId: {ExamId} -- From DetermineBillabityOfResultHandler, bills will be processed once pdf is delivered",
                determineBillability.Exam.ExamId);
            return;
        }

        //Move forward to billing as Pdf exists
        var billability = billableRules.IsBillable(new BillableRuleAnswers
        {
            Gradings = determineBillability.Gradings,
            ImageDetails = determineBillability.ImageDetails,
            HasEnucleation = determineBillability.Exam.HasEnucleation.GetValueOrDefault()
        });
        if (billability.IsMet)
        {
            logger.LogInformation("Exam : {ExamId} is complete, is billable and has pdfDelivered ", determineBillability.Exam.ExamId);
            await SendBillingRequest(determineBillability, pdfDataToClient, context);
        }
        else
        {
            // BillRequestNotSent
            await PublishBillRequestNotSent(determineBillability, pdfDataToClient);
            logger.LogInformation("ExamId: {ExamId} is not Billable. Either it doesn't have images for both eyes or none of the eyes were graded",
                determineBillability.Exam.ExamId);
        }
    }

    /// <summary>
    /// Handle Provider Payment
    /// </summary>
    /// <param name="exam"></param>
    /// <param name="context"></param>
    private async Task ProcessPayment(ExamModel exam, IMessageHandlerContext context)
    {
        if (!featureFlags.EnableProviderPayCdi)
        {
            logger.LogInformation("ProviderPay feature is NOT enabled for EvaluationId={EvaluationId}", exam.EvaluationId);
            return;
        }

        var examStatus = await mediator.Send(new GetLatestCdiEvent { EvaluationId = exam.EvaluationId }, context.CancellationToken);

        if (examStatus?.ExamStatusCodeId is (int)ExamStatusCode.StatusCodes.CdiPassedReceived or (int)ExamStatusCode.StatusCodes.CdiFailedWithPayReceived)
        {
            await SendProviderPayRequest(exam, examStatus, context);
        }

        logger.LogInformation("A valid CDI event has not been received for EvaluationId={EvaluationId}. ProviderPay will not be triggered",
            exam.EvaluationId);
    }

    private async Task PublishBillRequestNotSent(DetermineBillabityOfResult command, PdfToClientModel pdfModel)
    {
        var createStatusResponse =
            await mediator.Send(new CreateStatus(command.Exam.ExamId, ExamStatusCode.BillRequestNotSent.Name, applicationTime.UtcNow()));
        if (createStatusResponse.IsNew)
        {
            var status = mapper.Map<BillRequestNotSent>(command.Exam);
            status.PdfDeliveryDate = pdfModel.DeliveryDateTime;
            status.ReceivedDate = applicationTime.UtcNow();
            await mediator.Send(new PublishStatusUpdate(status));
        }
    }

    private async Task SendBillingRequest(DetermineBillabityOfResult command, PdfToClientModel pdfModel, IMessageHandlerContext context)
    {
        var billId = await mediator.Send(new GetRcmBillId(command.Exam.ExamId), context.CancellationToken).ConfigureAwait(false);

        //Make sure that RCM bill Id does not exist then fire RCMBillingRequestEvent.
        if (string.IsNullOrWhiteSpace(billId))
        {
            var rcmBillingRequest = new RCMBillingRequestEvent
            {
                ExamId = command.Exam.ExamId,
                EvaluationId = command.Exam.EvaluationId,
                ApplicationId = "signify.dee.service",
                RcmProductCode = EvaluationObjective.GetProductBillingCode(command.Exam.EvaluationObjective.Objective),
                BillableDate = pdfModel.DeliveryDateTime,
                SharedClientId = command.Exam.ClientId,
                CorrelationId = Guid.NewGuid().ToString(),
                ProviderId = command.Exam.ProviderId,
            };
            rcmBillingRequest.AdditionalDetails = new Dictionary<string, string>
            {
                { "BatchName", pdfModel.BatchName },
                { "EvaluationId", rcmBillingRequest.EvaluationId.ToString() }
            };

            if (command.Exam.AppointmentId != null)
            {
                rcmBillingRequest.AdditionalDetails.Add("appointmentId", command.Exam.AppointmentId.ToString());
            }

            await context.SendLocal(rcmBillingRequest);
            logger.LogInformation("RCMBillingRequest sent successfully from AddBillStatus handler for EvaluationId : {evaluationId}",
                command.Exam.EvaluationId);
        }
        else
        {
            logger.LogInformation("ExamId: {ExamId} -- has been billed already and has billId : {BillId}", command.Exam.ExamId, billId);
        }
    }

    /// <summary>
    /// Send NSB event to handle database write and kafka event
    /// </summary>
    /// <param name="exam"></param>
    /// <param name="examStatus"></param>
    /// <param name="context"></param>
    [Trace]
    private async Task SendProviderPayRequest(ExamModel exam, ExamStatus examStatus, IPipelineContext context)
    {
        var eventId = exam.RequestId ?? Guid.NewGuid();
        var providerPayEventRequest = mapper.Map<ProviderPayRequest>(exam);
        providerPayEventRequest.EventId = eventId;
        mapper.Map(examStatus, providerPayEventRequest);
        providerPayEventRequest.AdditionalDetails = new Dictionary<string, string>
        {
            { "EvaluationId", exam.EvaluationId.ToString() },
            { "ExamId", exam.ExamId.ToString() }
        };
        if (exam.AppointmentId != null)
        {
            providerPayEventRequest.AdditionalDetails.Add("appointmentId", exam.AppointmentId.ToString());
        }
        await context.SendLocal(providerPayEventRequest);
    }
    
}