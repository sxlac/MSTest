using System;
using System.Threading.Tasks;
using Signify.eGFR.Core.Data.Entities;
using Signify.eGFR.Core.Queries;
using Xunit;

namespace Signify.eGFR.Core.Tests.Queries;

public class QueryExamCenseoIdHandlerTest
{
    [Fact]
    public async Task Handle_WhenEntityDoesNotExist_ReturnsNull()
    {
        const string censeoId = "abc";
        DateTimeOffset collectionDate = DateTime.UtcNow;
        await using var fixture = new MockDbFixture();
        var subject = new QueryExamByCenseoIdHandler(fixture.SharedDbContext);
      
        var actual = await subject.Handle(new QueryExamByCenseoId(censeoId, collectionDate), default);

        Assert.Null(actual);
    }

    [Fact]
    public async Task Handle_WhenEntityExists_ReturnsEntity()
    {
        const string censeoId = "abc";
        DateTimeOffset collectionDate = DateTime.UtcNow;
        await using var fixture = new MockDbFixture();
        var exam = new Exam
        {
            ApplicationId = nameof(ApplicationId),
            CenseoId = censeoId,
            DateOfService = collectionDate
        };
        await fixture.SharedDbContext.Exams.AddAsync(exam);
        await fixture.SharedDbContext.SaveChangesAsync();
        var subject = new QueryExamByCenseoIdHandler(fixture.SharedDbContext);
        
        var actual = await subject.Handle(new QueryExamByCenseoId(censeoId, collectionDate), default);

        Assert.NotNull(actual);
    }
}