using Signify.AkkaStreams.Kafka.Persistence;

namespace Signify.FOBT.Svc.Core.Data
{
    /// <summary>
    /// Interface for getting a transaction. Encapsulates starting a new database transaction and
    /// supplying that transaction to the Akka message producer for use of the Outbox Pattern.
    /// </summary>
    /// <remarks>
    /// Starting and committing new database transactions explicitly via this interface is not
    /// necessary when all your database and event publishing logic is inside (or chained within)
    /// a single MediatR command. In this case, the MediatrUnitOfWork behavior will handle this
    /// for you.
    ///
    /// But, quite often, a single NSB event handler calls numerous MediatR commands in series.
    /// In this case, you need to use this interface and explicitly start a new transaction
    /// and commit at the end of your handle method so that a single transaction is used. If you
    /// do not, the MediatrUnitOfWork behavior will create a new transaction for each MediatR
    /// call, negating the benefits of the Unit of Work, as well as the related benefits of the
    /// Outbox feature of the Akka streams library.
    /// </remarks>
    public interface ITransactionSupplier
    {
        /// <summary>
        /// Starts a new transaction
        /// </summary>
        /// <returns></returns>
        IBufferedTransaction BeginTransaction();
    }
}
