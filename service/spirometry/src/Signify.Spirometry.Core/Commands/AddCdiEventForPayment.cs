using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.Spirometry.Core.Data;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Queries;

namespace Signify.Spirometry.Core.Commands;

public class AddCdiEventForPayment : IRequest<AddCdiEventForPaymentResponse>
{
    public CdiEventForPayment CdiEventForPayment { get; set; }

    public AddCdiEventForPayment(CdiEventForPayment cdiEventForPayment)
    {
        CdiEventForPayment = cdiEventForPayment;
    }
}

public class AddCdiEventForPaymentResponse
{
    public CdiEventForPayment CdiEventForPayment { get; set; }

    /// <summary>
    /// Whether or not this status was just inserted, or if it already existed in the database
    /// </summary>
    public bool IsNew { get; set; }

    public AddCdiEventForPaymentResponse(CdiEventForPayment cdiEvent, bool isNew)
    {
        CdiEventForPayment = cdiEvent;
        IsNew = isNew;
    }
}

public class AddCdiEventForPaymentHandler : IRequestHandler<AddCdiEventForPayment, AddCdiEventForPaymentResponse>
{
    private readonly ILogger<AddCdiEventForPaymentHandler> _logger;
    private readonly SpirometryDataContext _dataContext;
    private readonly IMediator _mediator;

    public AddCdiEventForPaymentHandler(ILogger<AddCdiEventForPaymentHandler> logger,
        SpirometryDataContext spirometryDataContext, IMediator mediator)
    {
        _logger = logger;
        _dataContext = spirometryDataContext;
        _mediator = mediator;
    }

    public async Task<AddCdiEventForPaymentResponse> Handle(AddCdiEventForPayment request, CancellationToken cancellationToken)
    {
        var entity = await FindExisting(request.CdiEventForPayment, cancellationToken);

        if (entity != null)
        {
            _logger.LogInformation(
                "A CdiEvent record with RequestId={RequestId} already exists, with BillRequestSentId={CdiEventId}, for EvaluationId={EvaluationId}",
                entity.RequestId, entity.CdiEventForPaymentId, request.CdiEventForPayment.EvaluationId);

            return new AddCdiEventForPaymentResponse(entity, false);
        }

        entity = (await _dataContext.CdiEventForPayments.AddAsync(request.CdiEventForPayment, cancellationToken).ConfigureAwait(false)).Entity;
        await _dataContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Successfully inserted a new CdiEvent record for EvaluationId={EvaluationId}, RequestId={RequestId} with new CdiEventId={CdiEventId}",
            entity.EvaluationId, entity.RequestId, entity.CdiEventForPaymentId);

        return new AddCdiEventForPaymentResponse(entity, true);
    }

    private async Task<CdiEventForPayment> FindExisting(CdiEventForPayment cdiEventForPayment, CancellationToken cancellationToken)
    {
        return await _mediator.Send(new QueryCdiEventForPayment(cdiEventForPayment.RequestId), cancellationToken);
    }
}