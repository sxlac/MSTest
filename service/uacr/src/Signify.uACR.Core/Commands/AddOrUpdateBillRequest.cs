using MediatR;
using Microsoft.Extensions.Logging;
using Signify.uACR.Core.Data.Entities;
using Signify.uACR.Core.Data;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace Signify.uACR.Core.Commands;

/// <summary>
/// Command to save a <see cref="BillRequest"/> to database
/// </summary>
public class AddOrUpdateBillRequest(Guid eventId, long evaluationId, BillRequest billRequest) : IRequest<BillRequest>
{
    /// <summary>
    /// Identifier of the event that resulted in this bill request
    /// </summary>
    public Guid EventId { get; } = eventId;

    /// <summary>
    /// Identifier of the evaluation corresponding to this <see cref="BillRequest"/>
    /// </summary>
    public long EvaluationId { get; } = evaluationId;

    /// <summary>
    /// Entity to save to database
    /// </summary>
    public BillRequest BillRequest { get; } = billRequest;

    public AddOrUpdateBillRequest(BillRequest billRequest) : this(new Guid(), 0, billRequest)
    {
    }
}

public class AddOrUpdateBillRequestHandler(
    ILogger<AddOrUpdateBillRequestHandler> logger,
    DataContext dataContext)
    : IRequestHandler<AddOrUpdateBillRequest, BillRequest>
{
    public async Task<BillRequest> Handle(AddOrUpdateBillRequest request, CancellationToken cancellationToken)
    {
        var entity = request.BillRequest.BillRequestId == 0
            ? (await dataContext.BillRequests.AddAsync(request.BillRequest, cancellationToken)).Entity
            : dataContext.BillRequests.Update(request.BillRequest).Entity;

        await dataContext.SaveChangesAsync(cancellationToken);

        var additionalInfo = request.EvaluationId > 0 ? $", for EvaluationId={request.EvaluationId}" : string.Empty;
        logger.LogInformation(
            "Successfully inserted/updated BillRequest record with BillId={BillId} - new BillRequestId={BillRequestId}{AdditionalInfo}",
            entity.BillId, entity.BillRequestId, additionalInfo);

        return entity;
    }
}