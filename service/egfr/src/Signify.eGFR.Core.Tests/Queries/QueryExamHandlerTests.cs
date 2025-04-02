using Signify.eGFR.Core.Data.Entities;
using Signify.eGFR.Core.Queries;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.eGFR.Core.Tests.Queries;

public class QueryExamHandlerTests
{
    [Fact]
    public async Task Handle_WhenEntityExist_ReturnsEntity()
    {
        const long evaluationId = 999;
        var request = new QueryExam(evaluationId);
        await using var fixture = new MockDbFixture();
        var expectedExam = new Exam
        {
            EvaluationId = evaluationId,
            ApplicationId = nameof(ApplicationId)
        };
        fixture.SharedDbContext.Exams.Add(expectedExam);
        await fixture.SharedDbContext.SaveChangesAsync();
        var subject = new QueryExamHandler(fixture.SharedDbContext);

        var actualResult = await subject.Handle(request, CancellationToken.None);

        Assert.Equal(expectedExam.EvaluationId, actualResult.EvaluationId);
    }

    [Fact]
    public async Task Handle_WhenEntityDoesNotExist_ReturnsNull()
    {
        const long evaluationId = 9001;
        var request = new QueryExam(evaluationId);
        await using var fixture = new MockDbFixture();
        var subject = new QueryExamHandler(fixture.SharedDbContext);

        var actualResult = await subject.Handle(request, CancellationToken.None);

        Assert.Null(actualResult);
    }
}