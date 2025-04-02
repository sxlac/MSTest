using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.CKD.Svc.Core.Data;

namespace Signify.CKD.Svc.Core.Queries;

public class GetProviderPayByCkdId : IRequest<Data.Entities.ProviderPay>
{
    public int CkdId { get; set; }
}

/// <summary>
/// Get CKD details from database.
/// </summary>
public class GetProviderPayByCkdIdHandler : IRequestHandler<GetProviderPayByCkdId, Data.Entities.ProviderPay>
{
    private readonly CKDDataContext _dataContext;
    private readonly ILogger<GetProviderPayByCkdIdHandler> _logger;
		
    public GetProviderPayByCkdIdHandler(CKDDataContext dataContext, ILogger<GetProviderPayByCkdIdHandler> logger)
    {
        _dataContext = dataContext;
        _logger = logger;
    }

    [Trace]
    public async Task<Data.Entities.ProviderPay> Handle(GetProviderPayByCkdId request, CancellationToken cancellationToken)
    {
        try
        {
            var providerPay = await _dataContext.ProviderPay.AsNoTracking().FirstOrDefaultAsync(s => s.CKD.CKDId == request.CkdId, cancellationToken: cancellationToken);
            return await Task.FromResult(providerPay);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ProviderPay with CkdId: {CkdId}", request.CkdId);
            throw;
        }
    }
}