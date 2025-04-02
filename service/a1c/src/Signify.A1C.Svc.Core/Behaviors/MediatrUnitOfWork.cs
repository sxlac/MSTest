using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Signify.AkkaStreams.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Signify.A1C.Svc.Core.Data;
using Signify.AkkaStreams.Postgres;

namespace Signify.A1C.Svc.Core.Behaviors
{
    public class MediatrUnitOfWork<TRequest, TResponse> :
            IPipelineBehavior<TRequest, TResponse>
    {
        private readonly ILogger _log;
        readonly IServiceProvider _serviceProvider;
        private readonly IMessageProducer _producer;

        public MediatrUnitOfWork(ILogger<TRequest> log, IServiceProvider serviceProvider, IMessageProducer producer)
        {
            _serviceProvider = serviceProvider;
            _log = log;
            _producer = producer;
        }

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            using var scope = _serviceProvider.CreateScope();
            var _context = scope.ServiceProvider.GetRequiredService<A1CDataContext>();
            if (_context.Database.CurrentTransaction != null)
            {
                _log.LogDebug($"Transaction is already open.");
                try
                {
                    _log.LogDebug($"Continuing Unit of Work");
                    var response = await next();
                    return response;
                }
                catch (Exception e)
                {
                    _log.LogDebug($"Rollback Unit of Work", e);
                    throw;
                }
            }
            using var transaction = _context.Database.BeginTransaction(_producer);
            try
            {
                var response = await next();
                _log.LogDebug("Committing Transaction");
                if (transaction != null) await transaction.CommitAsync(cancellationToken);
                return response;
            }
            catch (Exception e)
            {
                _log.LogDebug($"Rollback Unit of Work", e);
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

        }
    }
}