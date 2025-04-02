using Signify.PAD.Svc.Core.Data.Entities;
using Signify.PAD.Svc.Core.Queries;
using Signify.PAD.Svc.Core.Tests.Utilities;
using System.Threading.Tasks;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Queries;

public class QueryPdfDeliveredToClientTests
{
    [Fact]
    public async Task Handle_WhenEntityDoesNotExist_ReturnsNull()
    {
        // Arrange
        const int evaluationId = 1;

        await using var fixture = new MockDbFixture();

        var subject = new QueryPdfDeliveredToClientHandler(fixture.Context);

        // Act
        var result = await subject.Handle(new QueryPdfDeliveredToClient(evaluationId), default);

        // Assert
        Assert.Null(result.Entity);
    }

    [Fact]
    public async Task Handle_WhenEntityExists_ReturnsEntity()
    {
        // Arrange
        const int evaluationId = 1;

        await using var fixture = new MockDbFixture();

        var pdf = new PDFToClient
        {
            EvaluationId = evaluationId
        };

        await fixture.Context.PDFToClient.AddAsync(pdf);
        await fixture.Context.SaveChangesAsync();

        var subject = new QueryPdfDeliveredToClientHandler(fixture.Context);

        // Act
        var result = await subject.Handle(new QueryPdfDeliveredToClient(evaluationId), default);

        // Assert
        Assert.NotNull(result.Entity);
    }
}