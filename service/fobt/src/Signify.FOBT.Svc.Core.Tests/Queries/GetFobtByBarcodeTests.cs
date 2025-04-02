using Signify.FOBT.Svc.Core.Queries;
using Signify.FOBT.Svc.Core.Tests.Utilities;
using System.Threading.Tasks;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.Queries;

public class GetFobtByBarcodeTests
{
    [Fact]
    public async Task Handle_WhenNoBarcodeExist_ReturnsEmptyCollection()
    {
        // Arrange
        const string barcode = "randomBarcode";

        var request = new GetFobtByBarcode
        {
           Barcode = barcode,
        };

        await using var fixture = new MockDbFixture();

        // Act
        var result = await new GetFobtByBarcodeHandler(fixture.Context).Handle(request, default);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_WhenBarcodeExist_ReturnsFobt()
    {
        const string barcode = "01234567891234";

        var request = new GetFobtByBarcode
        {
            Barcode = barcode,
        };

        await using var fixture = new MockDbFixture();

        // Act
        var result = await new GetFobtByBarcodeHandler(fixture.Context).Handle(request, default);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
    }
}