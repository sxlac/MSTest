using System;
using System.Linq;
using AutoFixture;
using Microsoft.EntityFrameworkCore;
using Signify.FOBT.Svc.Core.Data;
using Signify.FOBT.Svc.Core.Data.Entities;

namespace Signify.FOBT.Svc.Core.Tests;

/// <summary>
/// This fixture can be used as a shared db context or as a backbone for individual per-test db contexts.
/// Your test class should implement IClassFixture<MockDbFixture> to use it.
/// Then, in each test either:
/// 1)  var dbContext = _dbFixture._sharedDbContext;  (to use the shared context)
/// or
/// 2) var dbContext = new SqlDataContext(_dbFixture.GetDbOptions("{uniquename}"));  (to use a unique context).
/// Shared contexts are faster but you must be careful that the test data inside the context cannot mess up another test.
/// Tests that test for specific *counts* of data records will need their own context, as it can't be guaranteed that no other test data might accidentally qualify.
/// Be sure to use the NextId function when assigning entity ids to avoid conflicts since the automated generation of ids
/// is not guaranteed to be unique.
/// As such, tests of code blocks that add new data records will circumvent the NextId feature and will need their own context.  
/// 
/// </summary>
public class MockDbFixture_old : IDisposable
{
    public readonly FOBTDataContext _sharedDbContext;
    public readonly Fixture _fixture;

    public MockDbFixture_old()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());//recursionDepth
        _sharedDbContext = new FOBTDataContext(GetDbOptions("Shared"));
    }
    private static readonly object idlock = new object();
    private static int _NextId = new Random().Next(10000, 999999);

    public int NextId
    {
        get
        {
            int returnval = 0;
            lock (idlock)
            {
                returnval = ++_NextId;
                return returnval;
            }

        }
    }

    public FOBTStatus MockUser()
    {
        //Generally you tell Fixture to create the user w/o the FK tables to avoid infinite recursion in generating properties, since
        //EF models tend to have circular virtual references.  Ours has no FK references though.
        //ex.  _fixture.Build<User>()
        //     .Without(x=>x.UserLanguage)
        //     .Without(x=>x.UserAddress)
        //     .Without...
        //     .Create();
        var user = _fixture.Build<FOBTStatus>().Create();
            
        //Override the primary key with a guaranteed-unique key to facilitate using shared dbContext when possible (for performance).
        user.FOBT.FOBTId = NextId;
        return user;
    }
    public DbContextOptions<FOBTDataContext> GetDbOptions(string dbname)
    {
        return new DbContextOptionsBuilder<FOBTDataContext>()
            .UseInMemoryDatabase(dbname)
            .EnableSensitiveDataLogging()
            .Options;
    }
        
    public void Dispose()
    {
            
    }
}