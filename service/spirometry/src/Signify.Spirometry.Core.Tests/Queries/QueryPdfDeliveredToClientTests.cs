using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Queries;
using System.Threading.Tasks;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Queries;

public class QueryPdfDeliveredToClientTests
{
    [Fact]
    public async Task Handle_WhenEntityDoesNotExist_ReturnsNull()
    {
        const int evaluationId = 1;

        await using var fixture = new MockDbFixture();

        var subject = new QueryPdfDeliveredToClientHandler(fixture.SharedDbContext);

        var result = await subject.Handle(new QueryPdfDeliveredToClient(evaluationId), default);

        Assert.Null(result.Entity);
    }

    [Fact]
    public async Task Handle_WhenEntityExists_ReturnsEntity()
    {
        const int evaluationId = 1;

        await using var fixture = new MockDbFixture();

        var billRequestSent = new PdfDeliveredToClient
        {
            EvaluationId = evaluationId
        };

        await fixture.SharedDbContext.PdfDeliveredToClients.AddAsync(billRequestSent);
        await fixture.SharedDbContext.SaveChangesAsync();

        var subject = new QueryPdfDeliveredToClientHandler(fixture.SharedDbContext);

        var result = await subject.Handle(new QueryPdfDeliveredToClient(evaluationId), default);

        Assert.NotNull(result.Entity);
    }
}