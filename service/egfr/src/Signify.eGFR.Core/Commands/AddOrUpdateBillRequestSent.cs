using MediatR;
using Microsoft.Extensions.Logging;
using Signify.eGFR.Core.Data;
using Signify.eGFR.Core.Data.Entities;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.eGFR.Core.Commands;

/// <summary>
/// Command to save a <see cref="BillRequestSent"/> to database
/// </summary>
[ExcludeFromCodeCoverage]
public class AddOrUpdateBillRequestSent(Guid eventId, long evaluationId, BillRequestSent billRequestSent)
    : IRequest<BillRequestSent>
{
    /// <summary>
    /// Identifier of the event that resulted in this bill request
    /// </summary>
    public Guid EventId { get; } = eventId;

    /// <summary>
    /// Identifier of the evaluation corresponding to this <see cref="BillRequestSent"/>
    /// </summary>
    public long EvaluationId { get; } = evaluationId;

    /// <summary>
    /// Entity to save to database
    /// </summary>
    public BillRequestSent BillRequestSent { get; } = billRequestSent;

    public AddOrUpdateBillRequestSent(BillRequestSent billRequestSent) : this(new Guid(), 0, billRequestSent)
    {
    }
}

public class AddOrUpdateBillRequestSentHandler(
    ILogger<AddOrUpdateBillRequestSentHandler> logger,
    DataContext dataContext)
    : IRequestHandler<AddOrUpdateBillRequestSent, BillRequestSent>
{
    private readonly ILogger _logger = logger;

    public async Task<BillRequestSent> Handle(AddOrUpdateBillRequestSent request, CancellationToken cancellationToken)
    {
        var entity = request.BillRequestSent.BillRequestSentId == 0
            ? (await dataContext.BillRequestSents.AddAsync(request.BillRequestSent, cancellationToken)).Entity
            : dataContext.BillRequestSents.Update(request.BillRequestSent).Entity;

        await dataContext.SaveChangesAsync(cancellationToken);

        var additionalInfo = request.EvaluationId > 0 ? $", for EvaluationId={request.EvaluationId}" : string.Empty;
        _logger.LogInformation(
            "Successfully inserted/updated BillRequestSent record with BillId={BillId} - new BillRequestSentId={BillRequestSentId}{AdditionalInfo}",
            entity.BillId, entity.BillRequestSentId, additionalInfo);

        return entity;
    }
}