using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;

namespace Signify.DEE.Svc.Core.Commands;

[ExcludeFromCodeCoverage]
public class CreateProviderPay : IRequest<Unit>
{
    public ProviderPay ProviderPay { get; set; }
}

public class CreateProviderPayHandler(DataContext context, ILogger<CreateProviderPayHandler> logger)
    : IRequestHandler<CreateProviderPay, Unit>
{
    [Transaction]
    public async Task<Unit> Handle(CreateProviderPay request, CancellationToken cancellationToken)
    {
        var entity = (await context.ProviderPays.AddAsync(request.ProviderPay, cancellationToken)).Entity;
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Successfully inserted a new ProviderPay record with CkdId={CkdId}, PaymentId={PaymentId} and ProviderPayId={ProviderPayId}",
            request.ProviderPay.ExamId, request.ProviderPay.PaymentId, entity.Id);

        return Unit.Value;
    }
}