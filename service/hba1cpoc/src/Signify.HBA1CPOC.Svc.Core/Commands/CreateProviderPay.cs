using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.HBA1CPOC.Svc.Core.Data;
using Signify.HBA1CPOC.Svc.Core.Data.Entities;

namespace Signify.HBA1CPOC.Svc.Core.Commands;

[ExcludeFromCodeCoverage]
public class CreateProviderPay : IRequest<Unit>
{
    public ProviderPay ProviderPay { get; set; }
}

public class CreateProviderPayHandler : IRequestHandler<CreateProviderPay, Unit>
{
    private readonly Hba1CpocDataContext _context;
    private readonly ILogger<CreateProviderPayHandler> _logger;

    public CreateProviderPayHandler(Hba1CpocDataContext context, ILogger<CreateProviderPayHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    [Transaction]
    public async Task<Unit> Handle(CreateProviderPay request, CancellationToken cancellationToken)
    {
        var entity = (await _context.ProviderPay.AddAsync(request.ProviderPay, cancellationToken)).Entity;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Successfully inserted a new ProviderPay record with HBA1CPOCId={HBA1CPOCId}, PaymentId={PaymentId} and ProviderPayId={ProviderPayId}",
            request.ProviderPay.HBA1CPOCId, request.ProviderPay.PaymentId, entity.ProviderPayId);

        return Unit.Value;
    }
}