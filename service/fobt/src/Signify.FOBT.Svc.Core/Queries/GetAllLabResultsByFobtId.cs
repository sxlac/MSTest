using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewRelic.Api.Agent;
using Signify.FOBT.Svc.Core.Data;
using Signify.FOBT.Svc.Core.Data.Entities;

namespace Signify.FOBT.Svc.Core.Queries;

public class GetAllLabResultsByFobtId : IRequest<List<LabResults>>
{
    public int FobtId { get; set; }

}

/// <summary>
/// Get all LabResults for a FobtId
/// </summary>
public class GetAllLabResultsByFobtIdHandler : IRequestHandler<GetAllLabResultsByFobtId, List<LabResults>>
{
    private readonly FOBTDataContext _dataContext;

    public GetAllLabResultsByFobtIdHandler(FOBTDataContext dataContext)
    {
        _dataContext = dataContext;
    }

    [Trace]
    public Task<List<LabResults>> Handle(GetAllLabResultsByFobtId request, CancellationToken cancellationToken)
    {
        return _dataContext.LabResults
            .AsNoTracking()
            .Where(each => each.FOBTId == request.FobtId)
            .ToListAsync(cancellationToken);
    }
}