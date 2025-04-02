using Signify.uACR.Core.Data.Entities;
using Signify.uACR.Core.Queries;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Xunit;

namespace Signify.uACR.Core.Tests.Queries;

public class GetExamByEvaluationHandlerTests
{
    [Fact]
    public async Task Handle_WhenEntityExists_IncludeStatusesFalse_ReturnsEntity()
    { 
        long evaluationId = 1;
        var queryExamByEvaluation = new QueryExamByEvaluation
        {
            EvaluationId = evaluationId,
            IncludeStatuses = false
        };

        await using var fixture = new MockDbFixture();

        var exam = new Exam
        {
            EvaluationId = evaluationId,
            ApplicationId = nameof(ApplicationId)
        };

        await fixture.SharedDbContext.Exams.AddAsync(exam);
        await fixture.SharedDbContext.SaveChangesAsync();

        var subject = new GetExamByEvaluationHandler(fixture.SharedDbContext);

        var result = await subject.Handle(queryExamByEvaluation, default);

        Assert.Equal(1, result.EvaluationId);
        Assert.Empty(result.ExamStatuses);
    }

    [Fact]
    public async Task Handle_WhenEntityExists_IncludeStatusesTrue_ReturnsEntity()
    { 
        long evaluationId = 1;
        var queryExamByEvaluation = new QueryExamByEvaluation
        {
            EvaluationId = evaluationId,
            IncludeStatuses = true
        };

        await using var fixture = new MockDbFixture();

        var exam = new Exam
        {
            EvaluationId = evaluationId,
            ApplicationId = nameof(ApplicationId),
            ExamStatuses = new List<ExamStatus>
            {
                new()
                {
                    ExamStatusId = 1,
                    ExamId = 1,
                    ExamStatusCodeId = 1,
                    StatusDateTime = DateTimeOffset.Now,
                    CreatedDateTime = DateTimeOffset.Now
                },
            }
        };

        await fixture.SharedDbContext.Exams.AddAsync(exam);
        await fixture.SharedDbContext.SaveChangesAsync();

        var subject = new GetExamByEvaluationHandler(fixture.SharedDbContext);

        var result = await subject.Handle(queryExamByEvaluation, default);

        Assert.Equal(1, result.EvaluationId);
        Assert.Single(result.ExamStatuses);
    }
}