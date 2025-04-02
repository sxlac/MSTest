using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using NServiceBus;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Data.Entities;
using Signify.PAD.Svc.Core.Events.Status;
using Signify.PAD.Svc.Core.Models;
using Signify.PAD.Svc.Core.Queries;
using Pad = Signify.PAD.Svc.Core.Data.Entities.PAD;

namespace Signify.PAD.Svc.Core.Events;

[Obsolete("To be removed in ANC-3978")]
[ExcludeFromCodeCoverage]
public abstract class ExamStatusEvent : ICommand
{
    public Guid EventId { get; set; }
    public long EvaluationId { get; set; }
    public int ExamId { get; set; }
    public PADStatusCode StatusCode { get; set; }
    public DateTimeOffset StatusDateTime { get; set; }
}

// ANC-3978 - rename this to just 'ExamStatusEvent'
// I can't do this now because it'd conflict with the above obsolete
// classname, and I can't change that to something like 'ExamStatusEventOld'
// because that's a breaking change since it's an NSB message
// that gets serialized...
[ExcludeFromCodeCoverage]
public class ExamStatusEventNew : IRequest
{
    public Guid EventId { get; set; }

    public Pad Exam { get; set; }

    public StatusCodes StatusCode { get; set; }

    public DateTimeOffset StatusDateTime { get; set; }
}

public class ExamStatusEventHandler : IRequestHandler<ExamStatusEventNew>
{
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;

    public ExamStatusEventHandler(IMapper mapper, IMediator mediator)
    {
        _mapper = mapper;
        _mediator = mediator;
    }

    public async Task Handle(ExamStatusEventNew request, CancellationToken cancellationToken)
    {
        await SaveStatusToDb(request, cancellationToken);

        var eventToPublish = await CreateKafkaStatusEvent(request, cancellationToken);

        if (eventToPublish != null)
            await _mediator.Send(new PublishStatusUpdate(request.EventId, eventToPublish), cancellationToken);
    }

    #region Kafka
    private async Task<BaseStatusMessage> CreateKafkaStatusEvent(ExamStatusEventNew request, CancellationToken cancellationToken)
    {
        switch (request.StatusCode)
        {
            case StatusCodes.PadPerformed:
            case StatusCodes.BillRequestSent:
            case StatusCodes.PadNotPerformed:
            case StatusCodes.ProviderPayableEventReceived:
            case StatusCodes.ProviderPayRequestSent:
            case StatusCodes.CdiPassedReceived:
            case StatusCodes.CdiFailedWithPayReceived:
            case StatusCodes.CdiFailedWithoutPayReceived:
            case StatusCodes.ProviderNonPayableEventReceived:
                return null; // To be implemented in ANC-3978
            case StatusCodes.BillRequestNotSent:
                var billRequestNotSent = _mapper.Map<BillRequestNotSent>(request.Exam);
                var pdf = (await _mediator.Send(new QueryPdfDeliveredToClient(request.Exam.EvaluationId!.Value), cancellationToken)).Entity;
                _mapper.Map(pdf, billRequestNotSent);
                return billRequestNotSent;
            case StatusCodes.BillableEventReceived:
            case StatusCodes.WaveformDocumentDownloaded:
            case StatusCodes.WaveformDocumentUploaded:
                return null; // None of these are events that need to be published to Kafka
            default:
                throw new NotImplementedException($"Status code {request.StatusCode} has not been handled");
        }
    }
    #endregion Kafka

    #region Database
    private Task SaveStatusToDb(ExamStatusEventNew request, CancellationToken cancellationToken)
    {
        var status = _mapper.Map<PADStatus>(request);
        return _mediator.Send(new AddExamStatus(status), cancellationToken);
    }
    #endregion Database
}
