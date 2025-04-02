using AutoFixture;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Signify.Spirometry.Core.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace Signify.Spirometry.Core.Tests;

/// <summary>
/// This fixture can be used as a shared db context or as a backbone for individual per-test db contexts.
/// Your test class should implement IClassFixture&lt;MockDbFixture&gt; to use it.
/// Then, in each test either:
/// 1)  var dbContext = _dbFixture._sharedDbContext;  (to use the shared context)
/// or
/// 2) var dbContext = new SqlDataContext(_dbFixture.GetDbOptions("{unique_name}"));  (to use a unique context).
/// Shared contexts are faster but you must be careful that the test data inside the context cannot mess up another test.
/// Tests that test for specific *counts* of data records will need their own context, as it can't be guaranteed that no other test data might accidentally qualify.
/// Be sure to use the NextId function when assigning entity ids to avoid conflicts since the automated generation of ids
/// is not guaranteed to be unique.
/// As such, tests of code blocks that add new data records will circumvent the NextId feature and will need their own context.
/// </summary>
public sealed class MockDbFixture : IAsyncDisposable, IDisposable
{
    public SpirometryDataContext SharedDbContext { get; }
    public Fixture Fixture { get; }

    /// <param name="databaseName">
    /// Optional in-memory database name to share the database between tests. If not specified, will use
    /// a different in-memory database per test.</param>
    public MockDbFixture(string databaseName = null)
    {
        Fixture = new Fixture();
        Fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => Fixture.Behaviors.Remove(b));
        Fixture.Behaviors.Add(new OmitOnRecursionBehavior());//recursionDepth

        databaseName ??= $"NonReusedDatabase_{GenerateNextId()}";

        SharedDbContext = new SpirometryDataContext(GetDbOptions(databaseName));
    }

    private static int _currentId;

    private static int GenerateNextId()
    {
        return Interlocked.Increment(ref _currentId);
    }

    private static DbContextOptions<SpirometryDataContext> GetDbOptions(string databaseName)
    {
        return new DbContextOptionsBuilder<SpirometryDataContext>()
            .UseInMemoryDatabase(databaseName)
            .EnableSensitiveDataLogging()
            .ConfigureWarnings(builder => builder.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
    }

    public void Dispose()
    {
        SharedDbContext.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return SharedDbContext.DisposeAsync();
    }
}