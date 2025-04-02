using Signify.uACR.Core.Data.Entities;
using Signify.uACR.Core.Queries;
using System.Threading.Tasks;
using System;
using Xunit;

namespace Signify.uACR.Core.Tests.Queries;

public class QueryPdfDeliveredToClientHandlerTests
{
    [Fact]
    public async Task Handle_WhenEntityExists_ReturnsEntity()
    {
        const long evaluationId = 1;
        await using var fixture = new MockDbFixture();

        var pdfDeliveredToClient = new PdfDeliveredToClient
        {
            PdfDeliveredToClientId = 1,
            EventId = Guid.Empty,
            EvaluationId = evaluationId,
            BatchId = 1,
            BatchName = "Testing",
            DeliveryDateTime = DateTimeOffset.Now,
            CreatedDateTime = DateTimeOffset.Now
        };

        await fixture.SharedDbContext.PdfDeliveredToClients.AddAsync(pdfDeliveredToClient);
        await fixture.SharedDbContext.SaveChangesAsync();

        var subject = new QueryPdfDeliveredToClientHandler(fixture.SharedDbContext);

        var actual = await subject.Handle(new QueryPdfDeliveredToClient(evaluationId), default);

        Assert.NotNull(actual);
    }
}