using System;
using System.Threading.Tasks;
using Signify.eGFR.Core.Data.Entities;
using Signify.eGFR.Core.Queries;
using Xunit;

namespace Signify.eGFR.Core.Tests.Queries;

public class QueryLabResultHandlerTests
{
    [Fact]
    public async Task Handle_WhenEntityDoesNotExist_ReturnsNull()
    {
        string censeoId = "abc";
        DateTimeOffset collectionDate = DateTime.UtcNow;

        await using var fixture = new MockDbFixture();

        var subject = new QueryQuestLabResultHandler(fixture.SharedDbContext);
        var actual = await subject.Handle(new QueryQuestLabResult(censeoId, collectionDate), default);

        Assert.Null(actual);
    }

    [Fact]
    public async Task Handle_WhenEntityExists_ReturnsEntity()
    {
        string censeoId = "abc";
        DateTimeOffset collectionDate = DateTime.UtcNow;

        await using var fixture = new MockDbFixture();
        var labResult = new QuestLabResult()
        {
            CenseoId = censeoId,
            CollectionDate = collectionDate
        };
        await fixture.SharedDbContext.QuestLabResults.AddAsync(labResult);
        await fixture.SharedDbContext.SaveChangesAsync();


        var subject = new QueryQuestLabResultHandler(fixture.SharedDbContext);
        var actual = await subject.Handle(new QueryQuestLabResult(censeoId, collectionDate), default);

        Assert.NotNull(actual);
    }
}