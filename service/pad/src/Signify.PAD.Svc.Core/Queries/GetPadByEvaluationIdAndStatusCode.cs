using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.PAD.Svc.Core.Data;
using Signify.PAD.Svc.Core.Data.Entities;

namespace Signify.PAD.Svc.Core.Queries;

public class GetPadByEvaluationIdAndStatusCode : IRequest<PADStatus>
{
    public long EvaluationId { get; set; }
    public int StatusCode { get; set; }
}

/// <summary>
/// Get PADStatus details, including PAD, from database.
/// </summary>
public class GetPadByEvaluationIdAndStatusCodeHandler : IRequestHandler<GetPadByEvaluationIdAndStatusCode, PADStatus>
{
    private readonly PADDataContext _dataContext;
    private readonly ILogger<GetPadByEvaluationIdAndStatusCodeHandler> _logger;

    public GetPadByEvaluationIdAndStatusCodeHandler(PADDataContext dataContext, ILogger<GetPadByEvaluationIdAndStatusCodeHandler> logger)
    {
        _dataContext = dataContext;
        _logger = logger;
    }

    [Transaction]
    public async Task<PADStatus> Handle(GetPadByEvaluationIdAndStatusCode request, CancellationToken cancellationToken)
    {
        try
        {
            var padDetails = await _dataContext.PADStatus
                .Include(t => t.PAD)
                .AsNoTracking()
                .FirstOrDefaultAsync(status =>
                    status.PAD.EvaluationId == request.EvaluationId
                    && status.PADStatusCodeId == request.StatusCode, cancellationToken: cancellationToken);
            return padDetails;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving PADStatus with EvaluationId: {EvaluationId}", request.EvaluationId);
            throw;
        }
    }
}