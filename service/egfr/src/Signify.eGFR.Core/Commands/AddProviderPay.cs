using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.eGFR.Core.Data;
using Signify.eGFR.Core.Data.Entities;

namespace Signify.eGFR.Core.Commands;

public class AddProviderPay : IRequest<Unit>
{
    public ProviderPay ProviderPay { get; set; }
}

public class CreateProviderPayHandler(DataContext context, ILogger<CreateProviderPayHandler> logger)
    : IRequestHandler<AddProviderPay, Unit>
{
    [Transaction]
    public async Task<Unit> Handle(AddProviderPay request, CancellationToken cancellationToken)
    {
        try
        {
            var entity = (await context.ProviderPay.AddAsync(request.ProviderPay, cancellationToken)).Entity;
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Successfully inserted a new ProviderPay record with ExamId={ExamId}, PaymentId={PaymentId} and Id={Id}",
                request.ProviderPay.ExamId, request.ProviderPay.PaymentId, entity.ProviderPayId);

            return Unit.Value;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Entry with payment id: {PaymentId} failed to be written to {Table} table",
                request.ProviderPay.PaymentId, nameof(ProviderPay));
            throw;
        }
    }
}