using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Queries;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Queries;

public class QuerySpirometryExamHandlerTests
{
    [Fact]
    public async Task Handle_WithRequest_QueriesDatabaseByEvaluationId()
    {
        const int evaluationId = 1;

        var request = new QuerySpirometryExam(evaluationId);

        await using var fixture = new MockDbFixture();

        var expectedExam = new SpirometryExam
        {
            EvaluationId = evaluationId
        };

        fixture.SharedDbContext.SpirometryExams.Add(expectedExam);
        await fixture.SharedDbContext.SaveChangesAsync();

        var subject = new QuerySpirometryExamHandler(fixture.SharedDbContext);

        var actualResult = await subject.Handle(request, CancellationToken.None);

        Assert.Equal(expectedExam, actualResult);
    }
}