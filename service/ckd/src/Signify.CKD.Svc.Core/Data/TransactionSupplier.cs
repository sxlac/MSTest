using Signify.AkkaStreams.Kafka;
using Signify.AkkaStreams.Kafka.Persistence;
using Signify.AkkaStreams.Postgres;

namespace Signify.CKD.Svc.Core.Data;

/// <summary>
/// Implementation of <see cref="ITransactionSupplier"/>
/// </summary>
public class TransactionSupplier : ITransactionSupplier
{
    private readonly CKDDataContext _ckdDataContext;
    private readonly IMessageProducer _producer;

    public TransactionSupplier(CKDDataContext ckdDataContext, IMessageProducer producer)
    {
        _ckdDataContext = ckdDataContext;
        _producer = producer;
    }

    /// <inheritdoc />
    public IBufferedTransaction BeginTransaction()
    {
        return _ckdDataContext.Database.BeginTransaction(_producer, autoCommit: false);
    }
}