using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.HBA1CPOC.Svc.Core.Data;

namespace Signify.HBA1CPOC.Svc.Core.Queries;

public class GetProviderPayByHba1CpocId: IRequest<Data.Entities.ProviderPay>
{
    public int HBA1CPOCId { get; set; }
}

/// <summary>
/// Get HBA1CPOC details from database.
/// </summary>
public class GetProviderPayByHba1CpocIdHandler : IRequestHandler<GetProviderPayByHba1CpocId, Data.Entities.ProviderPay>
{
    private readonly Hba1CpocDataContext _dataContext;
    private readonly ILogger<GetProviderPayByHba1CpocIdHandler> _logger;
		
    public GetProviderPayByHba1CpocIdHandler(Hba1CpocDataContext dataContext, ILogger<GetProviderPayByHba1CpocIdHandler> logger)
    {
        _dataContext = dataContext;
        _logger = logger;
    }

    [Trace]
    public async Task<Data.Entities.ProviderPay> Handle(GetProviderPayByHba1CpocId request, CancellationToken cancellationToken)
    {
        try
        {
            var providerPay = await _dataContext.ProviderPay.AsNoTracking().FirstOrDefaultAsync(s => s.HBA1CPOCId == request.HBA1CPOCId, cancellationToken: cancellationToken);
            return await Task.FromResult(providerPay);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ProviderPay with HBA1CPOCId: {HBA1CPOCId}", request.HBA1CPOCId);
            throw;
        }
    }
}