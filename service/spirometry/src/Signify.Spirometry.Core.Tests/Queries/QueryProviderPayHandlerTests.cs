using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Queries;
using System.Threading.Tasks;
using System;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Queries;

public class QueryProviderPayHandlerTests
{
    [Fact]
    public async Task Handle_WhenEntityDoesNotExist_ReturnsNull()
    {
        const long evaluationId = 12345;
        await using var fixture = new MockDbFixture();
        var subject = new QueryProviderPayHandler(fixture.SharedDbContext);

        var result = await subject.Handle(new QueryProviderPay(evaluationId), default);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_WhenEntityExists_ReturnsEntity()
    {
        const int evaluationId = 12345;
        var paymentId = Guid.NewGuid().ToString();
        await using var fixture = new MockDbFixture();
        var exam = new SpirometryExam
        {
            EvaluationId = evaluationId
        };
        exam = (await fixture.SharedDbContext.SpirometryExams.AddAsync(exam)).Entity;
        await fixture.SharedDbContext.SaveChangesAsync();
        var providerPay = new ProviderPay
        {
            SpirometryExam = exam,
            SpirometryExamId = exam.SpirometryExamId,
            PaymentId = paymentId
        };
        await fixture.SharedDbContext.ProviderPays.AddAsync(providerPay);
        await fixture.SharedDbContext.SaveChangesAsync();
        var subject = new QueryProviderPayHandler(fixture.SharedDbContext);

        var result = await subject.Handle(new QueryProviderPay(evaluationId), default);

        Assert.NotNull(result);
        Assert.Equal(paymentId, result.PaymentId);
    }
}