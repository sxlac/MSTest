using Signify.AkkaStreams.Kafka;
using Signify.AkkaStreams.Kafka.Persistence;
using Signify.AkkaStreams.Postgres;

namespace Signify.Spirometry.Core.Data
{
    public class TransactionSupplier : ITransactionSupplier
    {
        private readonly SpirometryDataContext _spirometryDataContext;
        private readonly IMessageProducer _producer;

        public TransactionSupplier(SpirometryDataContext spirometryDataContext,
            IMessageProducer producer)
        {
            _spirometryDataContext = spirometryDataContext;
            _producer = producer;
        }

        /// <inheritdoc />
        public IBufferedTransaction BeginTransaction()
        {
            return _spirometryDataContext.Database.BeginTransaction(_producer, autoCommit: false);
        }
    }
}
