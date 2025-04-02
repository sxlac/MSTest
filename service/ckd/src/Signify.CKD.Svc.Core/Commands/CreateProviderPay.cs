using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.CKD.Svc.Core.Data;
using Signify.CKD.Svc.Core.Data.Entities;

namespace Signify.CKD.Svc.Core.Commands;

public class CreateProviderPay : IRequest<Unit>
{
    public ProviderPay ProviderPay { get; set; }
}

public class CreateProviderPayHandler : IRequestHandler<CreateProviderPay, Unit>
{
    private readonly CKDDataContext _context;
    private readonly ILogger<CreateProviderPayHandler> _logger;

    public CreateProviderPayHandler(CKDDataContext context, ILogger<CreateProviderPayHandler> logger)
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
            "Successfully inserted a new ProviderPay record with CkdId={CkdId}, PaymentId={PaymentId} and ProviderPayId={ProviderPayId}",
            request.ProviderPay.CKDId, request.ProviderPay.PaymentId, entity.ProviderPayId);

        return Unit.Value;
    }
}