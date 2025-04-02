using Signify.AkkaStreams.Kafka;
using Signify.AkkaStreams.Kafka.Persistence;
using Signify.AkkaStreams.Postgres;

namespace Signify.DEE.Svc.Core.Data;

public class TransactionSupplier(
    DataContext dataDataContext,
    IMessageProducer producer) : ITransactionSupplier
{
    /// <inheritdoc />
    public IBufferedTransaction BeginTransaction()
    {
        return dataDataContext.Database.BeginTransaction(producer, autoCommit: false);
    }
}