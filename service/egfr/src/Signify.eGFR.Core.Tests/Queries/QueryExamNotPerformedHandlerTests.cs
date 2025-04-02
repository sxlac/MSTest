using Signify.eGFR.Core.Data.Entities;
using Signify.eGFR.Core.Queries;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Signify.eGFR.Core.Tests.Queries;

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
        var exam = new Exam
        {
            EvaluationId = evaluationId,
            ApplicationId = nameof(ApplicationId)
        };
        exam = (await fixture.SharedDbContext.Exams.AddAsync(exam)).Entity;
        await fixture.SharedDbContext.SaveChangesAsync();
        var notPerformed = new ExamNotPerformed
        {
            Exam = exam,
            ExamId = exam.ExamId,
            NotPerformedReason = NotPerformedReason.NotInterested
        };
        await fixture.SharedDbContext.ExamNotPerformeds.AddAsync(notPerformed);
        await fixture.SharedDbContext.SaveChangesAsync();
        var subject = new QueryExamNotPerformedHandler(fixture.SharedDbContext);

        var actual = await subject.Handle(new QueryExamNotPerformed(evaluationId), default);

        Assert.NotNull(actual);
    }
}