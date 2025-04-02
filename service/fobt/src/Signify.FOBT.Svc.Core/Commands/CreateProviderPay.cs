using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.FOBT.Svc.Core.Data;
using Signify.FOBT.Svc.Core.Data.Entities;

namespace Signify.FOBT.Svc.Core.Commands;

public class CreateProviderPay : IRequest<Unit>
{
    public ProviderPay ProviderPay { get; set; }
}

public class CreateProviderPayHandler : IRequestHandler<CreateProviderPay, Unit>
{
    private readonly FOBTDataContext _context;
    private readonly ILogger<CreateProviderPayHandler> _logger;

    public CreateProviderPayHandler(FOBTDataContext context, ILogger<CreateProviderPayHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    [Transaction]
    public async Task<Unit> Handle(CreateProviderPay request, CancellationToken cancellationToken)
    {
        try
        {
            var entity = (await _context.ProviderPay.AddAsync(request.ProviderPay, cancellationToken)).Entity;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully inserted a new ProviderPay record with FOBTId={FOBTId}, PaymentId={PaymentId} and Id={Id}",
                request.ProviderPay.FOBTId, request.ProviderPay.PaymentId, entity.Id);

            return Unit.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Entry with payment id: {PaymentId} failed to be written to {Table} table",
                request.ProviderPay.PaymentId, nameof(ProviderPay));
            throw;
        }
    }
}