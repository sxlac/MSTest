using Signify.AkkaStreams.Kafka;
using Signify.AkkaStreams.Kafka.Persistence;
using Signify.AkkaStreams.Postgres;

namespace Signify.uACR.Core.Data;

public class TransactionSupplier(
    DataContext dataContext,
    IMessageProducer producer) : ITransactionSupplier
{
    /// <inheritdoc />
    public IBufferedTransaction BeginTransaction() => dataContext.Database.BeginTransaction(producer, autoCommit: false);
}