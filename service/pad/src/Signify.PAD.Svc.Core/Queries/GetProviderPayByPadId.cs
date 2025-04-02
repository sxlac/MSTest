using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.PAD.Svc.Core.Data;

namespace Signify.PAD.Svc.Core.Queries;

public class GetProviderPayByPadId : IRequest<Data.Entities.ProviderPay>
{
    public int PadId { get; set; }
}

/// <summary>
/// Get PAD details from database.
/// </summary>
public class GetProviderPayByPadIdHandler : IRequestHandler<GetProviderPayByPadId, Data.Entities.ProviderPay>
{
    private readonly PADDataContext _dataContext;
    private readonly ILogger<GetProviderPayByPadIdHandler> _logger;
		
    public GetProviderPayByPadIdHandler(PADDataContext dataContext, ILogger<GetProviderPayByPadIdHandler> logger)
    {
        _dataContext = dataContext;
        _logger = logger;
    }

    [Trace]
    public async Task<Data.Entities.ProviderPay> Handle(GetProviderPayByPadId request, CancellationToken cancellationToken)
    {
        try
        {
            var providerPay = await _dataContext.ProviderPay.AsNoTracking().FirstOrDefaultAsync(s => s.PAD.PADId == request.PadId, cancellationToken: cancellationToken);
            return await Task.FromResult(providerPay);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ProviderPay with PadId: {PadId}", request.PadId);
            throw;
        }
    }
}