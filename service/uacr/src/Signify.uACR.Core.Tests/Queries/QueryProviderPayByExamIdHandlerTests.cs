using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.uACR.Core.Data.Entities;
using Signify.uACR.Core.Queries;
using System.Threading.Tasks;
using Xunit;

namespace Signify.uACR.Core.Tests.Queries;

public class QueryProviderPayByExamIdHandlerTests
{
    [Fact]
    public async Task Handle_WhenEntityExists_ReturnsEntity()
    {
        await using var fixture = new MockDbFixture();

        var exam = new Exam
        {
            ExamId = 74,
            ApplicationId = "uACR"
        };

        await fixture.SharedDbContext.Exams.AddAsync(exam);
        await fixture.SharedDbContext.SaveChangesAsync();

        var providerPay = new ProviderPay
        {
            ExamId = 74,
            PaymentId = "1776",
            Exam = exam
        };

        await fixture.SharedDbContext.ProviderPays.AddAsync(providerPay);
        await fixture.SharedDbContext.SaveChangesAsync();

        var subject = new QueryProviderPayByExamIdHandler(A.Dummy<ILogger<QueryProviderPayByExamIdHandler>>(), fixture.SharedDbContext);

        var actual = await subject.Handle(new QueryProviderPayByExamId{ExamId = 74}, default);

        Assert.NotNull(actual);
    }
}