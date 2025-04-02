using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.uACR.Core.Data;
using Signify.uACR.Core.Data.Entities;


namespace Signify.uACR.Core.Commands;


public class AddProviderPay : IRequest
{
    public ProviderPay ProviderPay { get; set; }
}

public class CreateProviderPayHandler(DataContext context, ILogger<CreateProviderPayHandler> logger)
    : IRequestHandler<AddProviderPay>
{
    [Transaction]
    public async Task Handle(AddProviderPay request, CancellationToken cancellationToken)
    {
        try
        {
            var entity = (await context.ProviderPays.AddAsync(request.ProviderPay, cancellationToken)).Entity;
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Successfully inserted a new ProviderPay record with ExamId={ExamId}, PaymentId={PaymentId} and Id={Id}",
                request.ProviderPay.ExamId, request.ProviderPay.PaymentId, entity.ProviderPayId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Entry with payment id: {PaymentId} failed to be written to {Table} table",
                request.ProviderPay.PaymentId, nameof(ProviderPay));
            throw;
        }
    }
}