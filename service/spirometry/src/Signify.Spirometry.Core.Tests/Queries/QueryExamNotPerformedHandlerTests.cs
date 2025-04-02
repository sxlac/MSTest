using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Queries;
using System.Threading.Tasks;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Queries;

public class QueryExamNotPerformedHandlerTests
{
    [Fact]
    public async Task Handle_WhenEntityDoesNotExist_ReturnsNull()
    {
        const int evaluationId = 1;

        await using var fixture = new MockDbFixture();

        var subject = new QueryExamNotPerformedHandler(fixture.SharedDbContext);

        var actual = await subject.Handle(new QueryExamNotPerformed(evaluationId), default);

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

        var notPerformedReason = new NotPerformedReason(1, 2, "Test");
           
        var notPerformed = new ExamNotPerformed
        {
            SpirometryExam = exam,
            SpirometryExamId = exam.SpirometryExamId,
            NotPerformedReason = notPerformedReason
        };

        await fixture.SharedDbContext.NotPerformedReasons.AddAsync(notPerformedReason);
        await fixture.SharedDbContext.ExamNotPerformeds.AddAsync(notPerformed);
        await fixture.SharedDbContext.SaveChangesAsync();

        var subject = new QueryExamNotPerformedHandler(fixture.SharedDbContext);

        var actual = await subject.Handle(new QueryExamNotPerformed(evaluationId), default);

        Assert.NotNull(actual);
    }
}