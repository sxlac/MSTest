using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Signify.AkkaStreams.Kafka;
using Signify.AkkaStreams.Kafka.Persistence;
using Signify.eGFR.Core.Data;
using Xunit;

namespace Signify.eGFR.Core.Tests;

public interface IFakeTransactionSupplier
{
    /// <summary>
    /// Asserts that a transaction was created and commit properly
    /// </summary>
    void AssertCommit();
    /// <summary>
    /// Asserts that a transaction was created and rolled back properly
    /// </summary>
    void AssertRollback();
    /// <summary>
    /// Asserts no transaction was created
    /// </summary>
    void AssertNoTransactionCreated();
}

public class FakeTransactionSupplier : ITransactionSupplier, IFakeTransactionSupplier
{
    private Transaction _transaction;

    /// <inheritdoc />
    public IBufferedTransaction BeginTransaction()
    {
        // As of now, we only have the need for a single transaction ever.
        // If this is to ever change, this can be expanded upon.
        var newTransaction = new Transaction();
        var existingTransaction = Interlocked.Exchange(ref _transaction, newTransaction);

        Assert.Null(existingTransaction);

        return newTransaction;
    }

    /// <inheritdoc />
    public void AssertCommit()
    {
        if (_transaction == null)
            Assert.Fail("No transaction was created");

        Assert.False(_transaction.DidThrow, "Code under test had an exception thrown when using a transaction, and the exception did not bubble up to the test");

        Assert.True(_transaction.DidCommit, "A transaction was expected to be commit, but was rolled back");
        Assert.True(_transaction.IsDisposed, "Code under test did not dispose of the transaction after commit");
    }

    /// <inheritdoc />
    public void AssertRollback()
    {
        if (_transaction == null)
            Assert.Fail("No transaction was created");

        Assert.False(_transaction.DidThrow, "Code under test had an exception thrown when using a transaction, and the exception did not bubble up to the test");

        Assert.False(_transaction.DidCommit, "A transaction was commit, but was expected to be rolled back");
        Assert.True(_transaction.IsDisposed, "Code under test did not dispose of the transaction after rollback");
    }

    /// <inheritdoc />
    public void AssertNoTransactionCreated()
    {
        if (_transaction != null)
            Assert.Fail("A transaction was created but not expected");
    }

    private sealed class Transaction : IBufferedTransaction
    {
        private readonly object _locker = new();
        private volatile bool _isDisposed;
        private volatile bool _didCommit;
        private volatile bool _didThrow; // In case the SUT throws and catches an exception that isn't raised to the test

        [SuppressMessage("ReSharper", "InconsistentlySynchronizedField")] // This is as designed
        public bool IsDisposed => _isDisposed;
        [SuppressMessage("ReSharper", "InconsistentlySynchronizedField")] // This is as designed
        public bool DidCommit => _didCommit;
        public bool DidThrow => _didThrow;

        public void Dispose()
        {
            lock (_locker)
            {
                _isDisposed = true;
            }
        }

        public void AddToBuffer(ProducableMessage message)
        {
            _didThrow = true;

            throw new NotImplementedException();
        }

        public Task CommitAsync(CancellationToken cancellationToken = new())
        {
            lock (_locker)
            {
                if (_isDisposed)
                    Throw("Attempted to commit after transaction was disposed");

                // We're testing our code here, and we don't expect this to ever happen; throw
                if (_didCommit)
                    Throw("Transaction has already commit");

                _didCommit = true;

                return Task.CompletedTask;
            }
        }

        public Task RollbackAsync(CancellationToken cancellationToken = new())
        {
            lock (_locker)
            {
                // We're testing our code here, and we don't expect this to ever happen; throw
                if (_isDisposed)
                    Throw("Attempted to rollback after transaction was disposed");

                if (_didCommit)
                    Throw("Attempted to rollback the transaction after it was commit");

                // No need to explicitly track a rollback; the code under test can explicitly rollback or implicitly by disposing the transaction

                return Task.CompletedTask;
            }
        }

        private void Throw(string message)
        {
            _didThrow = true;

            throw new InvalidOperationException(message);
        }

        public object DbTransaction { get; set; }
        public bool AutoCommit { get; set; }
    }
}