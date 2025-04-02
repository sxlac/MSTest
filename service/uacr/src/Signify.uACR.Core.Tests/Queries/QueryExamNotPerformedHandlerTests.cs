using Signify.uACR.Core.Data.Entities;
using Signify.uACR.Core.Queries;
using System.Threading.Tasks;
using System;
using Xunit;

namespace Signify.uACR.Core.Tests.Queries;

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
        
        var notPerformedReasons = NotPerformedReason.ScheduledToComplete;

        await fixture.SharedDbContext.NotPerformedReasons.AddAsync(notPerformedReasons);
        await fixture.SharedDbContext.SaveChangesAsync();
        
        exam = (await fixture.SharedDbContext.Exams.AddAsync(exam)).Entity;
        await fixture.SharedDbContext.SaveChangesAsync();

        var notPerformed = new ExamNotPerformed
        {
            Exam = exam,
            ExamId = exam.ExamId,
            NotPerformedReason = notPerformedReasons
        };

        await fixture.SharedDbContext.ExamNotPerformeds.AddAsync(notPerformed);
        await fixture.SharedDbContext.SaveChangesAsync();

        var subject = new QueryExamNotPerformedHandler(fixture.SharedDbContext);

        var actual = await subject.Handle(new QueryExamNotPerformed(evaluationId), default);

        Assert.NotNull(actual);
    }
}