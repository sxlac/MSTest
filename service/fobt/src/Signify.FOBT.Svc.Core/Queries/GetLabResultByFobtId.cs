using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Signify.FOBT.Svc.Core.Data;
using Signify.FOBT.Svc.Core.Data.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.FOBT.Svc.Core.Queries;

public class GetLabResultByFobtId : IRequest<LabResults>
{
    public int FobtId { get; set; }
}

public class GetLabResultByFobtIdHandler : IRequestHandler<GetLabResultByFobtId, LabResults>
{
    private readonly FOBTDataContext _dataContext;
    private readonly ILogger<GetLabResultHandler> _logger;
        
    public GetLabResultByFobtIdHandler(FOBTDataContext dataContext, ILogger<GetLabResultHandler> logger)
    {
        _dataContext = dataContext;
        _logger = logger;
    }

    public async Task<LabResults> Handle(GetLabResultByFobtId request, CancellationToken cancellationToken)
    {
        try
        {
            return await _dataContext.LabResults.AsNoTracking().FirstOrDefaultAsync(p => p.FOBTId == request.FobtId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving labResults with FOBT ID:{FobtId}", request.FobtId);
        }

        return null;
    }
}