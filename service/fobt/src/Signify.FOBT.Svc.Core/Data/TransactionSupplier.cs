using Signify.AkkaStreams.Kafka;
using Signify.AkkaStreams.Kafka.Persistence;
using Signify.AkkaStreams.Postgres;

namespace Signify.FOBT.Svc.Core.Data
{
    public class TransactionSupplier : ITransactionSupplier
    {
        private readonly FOBTDataContext _dataContext;
        private readonly IMessageProducer _producer;

        public TransactionSupplier(FOBTDataContext dataContext,
            IMessageProducer producer)
        {
            _dataContext = dataContext;
            _producer = producer;
        }

        /// <inheritdoc />
        public IBufferedTransaction BeginTransaction()
        {
            return _dataContext.Database.BeginTransaction(_producer, autoCommit: false);
        }
    }
}
