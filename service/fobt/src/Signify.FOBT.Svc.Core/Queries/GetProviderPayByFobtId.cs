using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.FOBT.Svc.Core.Data;

namespace Signify.FOBT.Svc.Core.Queries;

[ExcludeFromCodeCoverage]
public class GetProviderPayByFobtId : IRequest<Data.Entities.ProviderPay>
{
    public int FOBTId { get; set; }
}

/// <summary>
/// Get FOBT details from database.
/// </summary>
public class GetProviderPayByFobtIdHandler : IRequestHandler<GetProviderPayByFobtId, Data.Entities.ProviderPay>
{
    private readonly FOBTDataContext _dataContext;
    private readonly ILogger<GetProviderPayByFobtIdHandler> _logger;
	
    public GetProviderPayByFobtIdHandler(FOBTDataContext dataContext, ILogger<GetProviderPayByFobtIdHandler> logger)
    {
        _dataContext = dataContext;
        _logger = logger;
    }

    [Trace]
    public async Task<Data.Entities.ProviderPay> Handle(GetProviderPayByFobtId request, CancellationToken cancellationToken)
    {
        try
        {
            var providerPay = await _dataContext.ProviderPay.AsNoTracking().FirstOrDefaultAsync(s => s.FOBTId == request.FOBTId, cancellationToken: cancellationToken);
            return await Task.FromResult(providerPay);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ProviderPay with FobtId: {FOBTId}", request.FOBTId);
            throw;
        }
    }
}