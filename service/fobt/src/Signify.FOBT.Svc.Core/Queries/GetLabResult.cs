using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Signify.FOBT.Svc.Core.Data;
using System;
using System.Threading;
using System.Threading.Tasks;
using NewRelic.Api.Agent;

namespace Signify.FOBT.Svc.Core.Queries;

public class GetLabResult : IRequest<Data.Entities.LabResults>
{
    public Guid? OrderCorrelationId { get; set; }
}

/// <summary>
/// Get FOBT details from database.
/// </summary>
public class GetLabResultHandler : IRequestHandler<GetLabResult, Data.Entities.LabResults>
{
    private readonly FOBTDataContext _dataContext;
    private readonly ILogger<GetLabResultHandler> _logger;
    public GetLabResultHandler(FOBTDataContext dataContext, ILogger<GetLabResultHandler> logger)
    {
        _dataContext = dataContext;
        _logger = logger;
    }

    [Trace]
    public async Task<Data.Entities.LabResults> Handle(GetLabResult request, CancellationToken cancellationToken)
    {
        try
        {
            var labResults = await _dataContext.LabResults.AsNoTracking().FirstOrDefaultAsync(s => s.OrderCorrelationId == request.OrderCorrelationId, cancellationToken: cancellationToken);
            return await Task.FromResult(labResults);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error retrieving labResults: {@ex}", ex);
            return null;
        }
    }      
}