using MediatR;
using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka;
using Signify.AkkaStreams.Postgres;
using Signify.uACR.Core.Data;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.uACR.Core.Behaviors;

public class MediatrUnitOfWork<TRequest, TResponse>(
    ILogger<MediatrUnitOfWork<TRequest, TResponse>> logger,
    DataContext context,
    IMessageProducer producer)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (context.Database.CurrentTransaction != null)
        {
            logger.LogDebug("Transaction is already open");
            try
            {
                logger.LogDebug("Continuing Unit of Work");
                var response = await next();
                return response;
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Error during Unit of Work");
                throw;
            }
        }

        using var transaction = context.Database.BeginTransaction(producer);
        try
        {
            var response = await next();
            logger.LogDebug("Committing Transaction");
            await transaction.CommitAsync(cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Rolling back Unit of Work");
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}