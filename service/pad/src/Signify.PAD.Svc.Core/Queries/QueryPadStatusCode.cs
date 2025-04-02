using MediatR;
using Microsoft.EntityFrameworkCore;
using Signify.PAD.Svc.Core.Data;
using System;
using System.Threading;
using System.Threading.Tasks;
using Signify.PAD.Svc.Core.Data.Entities;
using Microsoft.Extensions.Logging;

namespace Signify.PAD.Svc.Core.Queries;

/// <summary>
/// Query to determine whether PAD has specified PADStatusCode
/// </summary>
public class QueryPadStatusCode : IRequest<bool>
{
    public int PadId { get; }
    public PADStatusCode PadStatusCode { get; }

    public QueryPadStatusCode(int padId, PADStatusCode padStatusCode)
    {
        PadId = padId;
        PadStatusCode = padStatusCode;
    }
}

public class QueryPadStatusCodeHandler : IRequestHandler<QueryPadStatusCode, bool>
{
    private readonly PADDataContext _dataContext;
    private readonly ILogger _logger;

    public QueryPadStatusCodeHandler(ILogger<QueryPadStatusCode> logger, PADDataContext dataContext)
    {
        _dataContext = dataContext;
        _logger = logger;
    }

    public async Task<bool> Handle(QueryPadStatusCode request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _dataContext.PADStatus
                .AsNoTracking()
                .Include(status => status.PAD)
                .Include(status => status.PADStatusCode)
                .FirstOrDefaultAsync(each => each.PAD.PADId == request.PadId && each.PADStatusCode.PADStatusCodeId == request.PadStatusCode.PADStatusCodeId, cancellationToken) is not null;

            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to find PADStatusCode {StatusCode} for PadId {PADId} due to an error: {Message}", request.PadStatusCode.StatusCode, request.PadId, e.Message);
        }
        return false;
    }
}