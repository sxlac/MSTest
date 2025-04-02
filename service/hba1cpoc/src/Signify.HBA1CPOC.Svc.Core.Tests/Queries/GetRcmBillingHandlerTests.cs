using FluentAssertions;
using Signify.HBA1CPOC.Svc.Core.Queries;
using Signify.HBA1CPOC.Svc.Core.Tests.Utilities;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.HBA1CPOC.Svc.Core.Tests.Queries;

public class GetRcmBillingHandlerTests : IClassFixture<MockDbFixture>
{
    private readonly GetRcmBillingHandler _handler;

    public GetRcmBillingHandlerTests(MockDbFixture mockDbFixture)
    {
        _handler = new GetRcmBillingHandler(mockDbFixture.Context);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task GetRcmBillingHandler_RequestCorrectHbA1cPocId_SuccessfulResponse(int id)
    {
        // Arrange
        var query = new GetRcmBilling() { HbA1cPocId = id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }

    [Theory]
    [InlineData(1231)]
    [InlineData(1232)]
    [InlineData(1233)]
    public async Task GetRcmBillingHandler_RequestIncorrectHbA1cPocId_NullResponse(int id)
    {
        // Arrange
        var query = new GetRcmBilling() { HbA1cPocId = id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}