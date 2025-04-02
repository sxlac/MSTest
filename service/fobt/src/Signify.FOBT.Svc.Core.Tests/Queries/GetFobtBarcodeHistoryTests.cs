using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Signify.FOBT.Svc.Core.Queries;
using Signify.FOBT.Svc.Core.Tests.Mocks.Models;
using Signify.FOBT.Svc.Core.Tests.Utilities;
using System.Threading.Tasks;
using System;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.Queries;

public class GetFobtBarcodeHistoryTests : IClassFixture<MockDbFixture>
{
    private readonly GetFobtBarcodeHistoryHandler _handler;

    public GetFobtBarcodeHistoryTests(MockDbFixture mockDbFixture)
    {
        var logger = A.Fake<ILogger<GetFobtBarcodeHistoryHandler>>();

        _handler = new GetFobtBarcodeHistoryHandler(logger, mockDbFixture.Context);
    }

    [Fact]
    public async Task Handle_RequestContainsInvalidHistoryData_ReturnsNull()
    {
        // Arrange
        var request = new GetFobtBarcodeHistory 
        { 
            Barcode = "barcode1234",
            OrderCorrelationId = Guid.NewGuid()
        };

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_RequestContainsValidHistoryData_ReturnFobtBarcodeHistoryRecord()
    {
        // Arrange
        var mockFobtBarcode = FobtBarcodeHistoryMock.BuildFobtBarcodeHistory();
        var request = new GetFobtBarcodeHistory
        {
            Barcode = mockFobtBarcode.Barcode,
            OrderCorrelationId = mockFobtBarcode.OrderCorrelationId!.Value
        };

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        result.Should().NotBeNull();
        result.Barcode.Should().Be(mockFobtBarcode.Barcode);
        result.OrderCorrelationId.Should().Be(mockFobtBarcode.OrderCorrelationId.Value);
    }
}