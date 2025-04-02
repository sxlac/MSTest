using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Queries;
using System.Threading.Tasks;
using System;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Queries;

public class QueryBillRequestSentByBillIdHandlerTests 
{
    [Fact]
    public async Task Handle_WhenEntityDoesNotExist_ReturnsNull()
    {
        var billId = Guid.NewGuid();
        
        await using var fixture = new MockDbFixture();

        var subject = new QueryBillRequestSentByBillIdHandler(fixture.SharedDbContext);

        var result = await subject.Handle(new QueryBillRequestSentByBillId(billId), default);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_WhenEntityExists_ReturnsEntity()
    {
        const int evaluationId = 1;
        var billId = Guid.NewGuid();
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
            SpirometryExamId = exam.SpirometryExamId,
            BillId = billId
        };

        await fixture.SharedDbContext.BillRequestSents.AddAsync(billRequestSent);
        await fixture.SharedDbContext.SaveChangesAsync();

        var subject = new QueryBillRequestSentByBillIdHandler(fixture.SharedDbContext);

        var result = await subject.Handle(new QueryBillRequestSentByBillId(billId), default);

        Assert.NotNull(result);
    }
}