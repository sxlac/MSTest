using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.Spirometry.Core.Data;
using Signify.Spirometry.Core.Data.Entities;

namespace Signify.Spirometry.Core.Commands;

public class AddProviderPay : IRequest<ProviderPay>
{
    public ProviderPay ProviderPay { get; set; }

    public AddProviderPay(ProviderPay providerPay)
    {
        ProviderPay = providerPay;
    }
}

public class AddProviderPayHandler : IRequestHandler<AddProviderPay, ProviderPay>
{
    private readonly ILogger<AddProviderPayHandler> _logger;
    private readonly SpirometryDataContext _spirometryDataContext;

    public AddProviderPayHandler(ILogger<AddProviderPayHandler> logger, SpirometryDataContext spirometryDataContext)
    {
        _spirometryDataContext = spirometryDataContext;
        _logger = logger;
    }

    [Transaction]
    public async Task<ProviderPay> Handle(AddProviderPay request, CancellationToken cancellationToken)
    {
        try
        {
            var entity = (await _spirometryDataContext.ProviderPays.AddAsync(request.ProviderPay, cancellationToken)).Entity;
            await _spirometryDataContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Propose to add a new ProviderPay record with ExamId={ExamId}, PaymentId={PaymentId} and Id={Id}",
                request.ProviderPay.SpirometryExamId, request.ProviderPay.PaymentId, entity.ProviderPayId);
            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Entry with payment id={PaymentId} failed to be written to {Table} table",
                request.ProviderPay.PaymentId, nameof(ProviderPay));
            throw;
        }
    }
}