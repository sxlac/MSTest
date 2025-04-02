using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Queries;
using System.Threading.Tasks;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Queries;

public class QueryExamResultsHandlerTests
{
    [Fact]
    public async Task Handle_WhenEntityDoesNotExist_ReturnsNull()
    {
        const int evaluationId = 1;

        await using var fixture = new MockDbFixture();

        var subject = new QueryExamResultsHandler(fixture.SharedDbContext);

        var actual = await subject.Handle(new QueryExamResults(evaluationId), default);

        Assert.Null(actual);
    }

    [Fact]
    public async Task Handle_WhenEntityExists_ReturnsEntity()
    {
        const int evaluationId = 1;

        await using var fixture = new MockDbFixture();

        var exam = new SpirometryExam
        {
            EvaluationId = evaluationId
        };

        exam = (await fixture.SharedDbContext.SpirometryExams.AddAsync(exam)).Entity;
        await fixture.SharedDbContext.SaveChangesAsync();

        var notPerformed = new SpirometryExamResult
        {
            SpirometryExam = exam,
            SpirometryExamId = exam.SpirometryExamId
        };

        await fixture.SharedDbContext.SpirometryExamResults.AddAsync(notPerformed);
        await fixture.SharedDbContext.SaveChangesAsync();

        var subject = new QueryExamResultsHandler(fixture.SharedDbContext);

        var actual = await subject.Handle(new QueryExamResults(evaluationId), default);

        Assert.NotNull(actual);
    }
}