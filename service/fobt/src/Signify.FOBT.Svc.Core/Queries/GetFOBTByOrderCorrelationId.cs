using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Signify.FOBT.Svc.Core.Data;
using System;
using System.Threading;
using System.Threading.Tasks;
using NewRelic.Api.Agent;

namespace Signify.FOBT.Svc.Core.Queries;

public class GetFobtByOrderCorrelationId : IRequest<Data.Entities.FOBT>
{
    public Guid OrderCorrelationId { get; set; }
}

/// <summary>
/// Get FOBT details from database.
/// </summary>
public class GetFobtByOrderCorrelationIdHandler : IRequestHandler<GetFobtByOrderCorrelationId, Data.Entities.FOBT>
{
    private readonly FOBTDataContext _dataContext;
    private readonly ILogger<GetFobtByOrderCorrelationId> _logger;
    public GetFobtByOrderCorrelationIdHandler(FOBTDataContext dataContext, ILogger<GetFobtByOrderCorrelationId> logger)
    {
        _dataContext = dataContext;
        _logger = logger;
    }

    [Trace]
    public async Task<Data.Entities.FOBT> Handle(GetFobtByOrderCorrelationId request, CancellationToken cancellationToken)
    {
        try
        {
            var fobt = await _dataContext.FOBT.AsNoTracking().FirstOrDefaultAsync(s => s.OrderCorrelationId == request.OrderCorrelationId, cancellationToken: cancellationToken);
            return await Task.FromResult(fobt);
        }

        catch (Exception ex)
        {
            _logger.LogError("Error retrieving FOBTs by OrderCorrelationId: {@ex}", ex);
            return null;
        }
    }
}