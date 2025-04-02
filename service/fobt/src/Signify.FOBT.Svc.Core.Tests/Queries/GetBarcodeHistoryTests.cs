using Signify.FOBT.Svc.Core.Queries;
using Signify.FOBT.Svc.Core.Tests.Utilities;
using System.Threading.Tasks;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.Queries;

public class GetBarcodeHistoryHandlerTests
{
    [Fact]
    public async Task Handle_WhenNoHistoryExist_ReturnsEmptyCollection()
    {
        // Arrange
        const string barcode = "randomBarcode";

        var request = new GetBarcodeHistory
        {
           Barcode = barcode,
        };

        await using var fixture = new MockDbFixture();

        // Act
        var result = await new GetBarcodeHistoryHandler(fixture.Context).Handle(request, default);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_WhenHistoryExist_ReturnsHistory()
    {
        const string barcode = "01234567890";

        var request = new GetBarcodeHistory
        {
            Barcode = barcode,
        };

        await using var fixture = new MockDbFixture();

        // Act
        var result = await new GetBarcodeHistoryHandler(fixture.Context).Handle(request, default);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
    }
}