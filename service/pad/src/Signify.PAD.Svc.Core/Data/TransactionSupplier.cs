using Signify.AkkaStreams.Kafka;
using Signify.AkkaStreams.Kafka.Persistence;
using Signify.AkkaStreams.Postgres;

namespace Signify.PAD.Svc.Core.Data;

public class TransactionSupplier : ITransactionSupplier
{
    private readonly PADDataContext _padDataContext;
    private readonly IMessageProducer _producer;

    public TransactionSupplier(PADDataContext padDataContext, IMessageProducer producer)
    {
        _padDataContext = padDataContext;
        _producer = producer;
    }

    /// <inheritdoc />
    public IBufferedTransaction BeginTransaction()
    {
        return _padDataContext.Database.BeginTransaction(_producer, autoCommit: false);
    }
}