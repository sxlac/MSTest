using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Queries;
using System.Threading.Tasks;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Queries;

public class QueryHoldHandlerTests
{
    [Fact]
    public async Task Handle_Test()
    {
        // Arrange
        const int evaluationId = 1;

        var request = new QueryHold
        {
            EvaluationId = evaluationId
        };

        await using var fixture = new MockDbFixture();

        await fixture.SharedDbContext.Holds.AddAsync(new Hold
        {
            EvaluationId = evaluationId
        });

        await fixture.SharedDbContext.SaveChangesAsync();

        var subject = new QueryHoldHandler(fixture.SharedDbContext);

        // Act
        var actual = await subject.Handle(request, default);

        // Assert
        Assert.NotNull(actual);
    }
}