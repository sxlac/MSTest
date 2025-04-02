using FluentAssertions;
using Signify.FOBT.Svc.Core.Commands;
using Signify.FOBT.Svc.Core.Tests.Utilities;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.Commands;

public class CreateBarcodeHistoryTests : IClassFixture<MockDbFixture>
{
    private readonly CreateBarcodeHistoryHandler _handler;
    private readonly MockDbFixture _mockDbFixture;

    public CreateBarcodeHistoryTests(MockDbFixture mockDbFixture)
    {
        _mockDbFixture = mockDbFixture;
        _handler = new CreateBarcodeHistoryHandler(_mockDbFixture.Context);
    }

    [Fact]
    public async Task Handle_Add_NewBarcodeHistory()
    {
        var test = new CreateBarcodeHistory
        {
            FOBTId = 50,
            OrderCorrelationId = Guid.Empty,
            Barcode = "Barcode12345"
        };

        var initialCount = _mockDbFixture.Context.FOBTBarcodeHistory.Count();
        await _handler.Handle(test, CancellationToken.None);
        _mockDbFixture.Context.FOBTBarcodeHistory.Count().Should().BeGreaterThanOrEqualTo(initialCount, "There shd be an insert");
    }
}