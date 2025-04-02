using Signify.AkkaStreams.Kafka;
using Signify.AkkaStreams.Kafka.Persistence;
using Signify.AkkaStreams.Postgres;

namespace Signify.HBA1CPOC.Svc.Core.Data;

/// <summary>
/// Implementation of <see cref="ITransactionSupplier"/>
/// </summary>
public class TransactionSupplier : ITransactionSupplier
{
    private readonly Hba1CpocDataContext _hba1CpocDataContext;
    private readonly IMessageProducer _producer;

    public TransactionSupplier(Hba1CpocDataContext hba1CpocDataContext, IMessageProducer producer)
    {
        _hba1CpocDataContext = hba1CpocDataContext;
        _producer = producer;
    }

    /// <inheritdoc />
    public IBufferedTransaction BeginTransaction()
    {
        return _hba1CpocDataContext.Database.BeginTransaction(_producer, autoCommit: false);
    }
}