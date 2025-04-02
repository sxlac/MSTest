using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka;
using Signify.AkkaStreams.Postgres;
using Signify.HBA1CPOC.Svc.Core.Data;

namespace Signify.HBA1CPOC.Svc.Core.Behaviors
{
    public class MediatrUnitOfWork<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger _logger;
        private readonly Hba1CpocDataContext _context;
        private readonly IMessageProducer _producer;

        public MediatrUnitOfWork(ILogger<MediatrUnitOfWork<TRequest, TResponse>> logger,
            Hba1CpocDataContext context,
            IMessageProducer producer)
        {
            _logger = logger;
            _context = context;
            _producer = producer;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (_context.Database.CurrentTransaction != null)
            {
                _logger.LogDebug("Transaction is already open");
                try
                {
                    _logger.LogDebug("Continuing Unit of Work");
                    var response = await next();
                    return response;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error during Unit of Work");
                    throw;
                }
            }

            using var transaction = _context.Database.BeginTransaction(_producer);
            try
            {
                var response = await next();
                _logger.LogDebug("Committing Transaction");
                await transaction.CommitAsync(cancellationToken);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Rolling back Unit of Work");
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}