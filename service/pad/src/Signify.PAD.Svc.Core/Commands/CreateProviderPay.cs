using MediatR;
using NewRelic.Api.Agent;
using Signify.PAD.Svc.Core.Data;
using Signify.PAD.Svc.Core.Data.Entities;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Signify.PAD.Svc.Core.Commands;

public class CreateProviderPay : IRequest<Unit>
{
    public ProviderPay ProviderPay { get; set; }
}

public class CreateProviderPayHandler : IRequestHandler<CreateProviderPay, Unit>
{
    private readonly PADDataContext _context;
    private readonly ILogger<CreateProviderPayHandler> _logger;

    public CreateProviderPayHandler(PADDataContext context, ILogger<CreateProviderPayHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    [Trace]
    public async Task<Unit> Handle(CreateProviderPay request, CancellationToken cancellationToken)
    {
        var entity = (await _context.ProviderPay.AddAsync(request.ProviderPay, cancellationToken)).Entity;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Successfully inserted a new ProviderPay record with PadId={PadId}, PaymentId={PaymentId} and ProviderPayId={ProviderPayId}",
            request.ProviderPay.PADId, request.ProviderPay.PaymentId, entity.ProviderPayId);

        return Unit.Value;
    }
}