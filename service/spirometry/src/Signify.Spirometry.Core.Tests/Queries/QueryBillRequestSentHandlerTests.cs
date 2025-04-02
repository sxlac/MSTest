using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Queries;
using System.Threading.Tasks;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Queries;

public class QueryBillRequestSentHandlerTests
{
    [Fact]
    public async Task Handle_WhenEntityDoesNotExist_ReturnsNull()
    {
        const int evaluationId = 1;

        await using var fixture = new MockDbFixture();

        var subject = new QueryBillRequestSentHandler(fixture.SharedDbContext);

        var result = await subject.Handle(new QueryBillRequestSent(evaluationId), default);

        Assert.Null(result.Entity);
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

        var billRequestSent = new BillRequestSent
        {
            SpirometryExam = exam,
            SpirometryExamId = exam.SpirometryExamId
        };

        await fixture.SharedDbContext.BillRequestSents.AddAsync(billRequestSent);
        await fixture.SharedDbContext.SaveChangesAsync();

        var subject = new QueryBillRequestSentHandler(fixture.SharedDbContext);

        var result = await subject.Handle(new QueryBillRequestSent(evaluationId), default);

        Assert.NotNull(result.Entity);
    }
}