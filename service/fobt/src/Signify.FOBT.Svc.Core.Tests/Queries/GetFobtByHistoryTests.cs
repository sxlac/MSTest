using FakeItEasy;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.FOBT.Svc.Core.Data.Entities;
using Signify.FOBT.Svc.Core.Queries;
using Signify.FOBT.Svc.Core.Tests.Mocks.Models;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.Queries;

public class GetFobtByHistoryTests
{
    private readonly IMediator _mediator;
    private readonly GetFobtByHistoryHandler _handler;

    public GetFobtByHistoryTests()
    {
        _mediator = A.Fake<IMediator>();
        var logger = A.Fake<ILogger<GetFobtByHistoryHandler>>();

        _handler = new GetFobtByHistoryHandler(_mediator, logger);
    }

    [Fact]
    public async Task Handle_RecordExistsInFobtBarcodeHistoryAndFobt_ReturnFobtRecord()
    {
        // Arrange
        var request = new GetFobtByHistory
        { 
            Barcode = "01234567890",
            OrderCorrelationId = Guid.NewGuid()
        };
        var fobtRecord = FobtEntityMock.BuildFobt();

        A.CallTo(() => _mediator.Send(A<GetFobtBarcodeHistory>._, A<CancellationToken>._)).Returns(FobtBarcodeHistoryMock.BuildFobtBarcodeHistory());
        A.CallTo(() => _mediator.Send(A<GetFobtByFobtId>._, A<CancellationToken>._)).Returns(fobtRecord);

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        result.Should().NotBeNull();
        result.FOBTId.Should().Be(fobtRecord.FOBTId);
    }

    [Fact]
    public async Task Handle_RecordDoesNotExistsInFobtBarcode_ReturnNull()
    {
        // Arrange
        var request = new GetFobtByHistory
        {
            Barcode = "barcode1234",
            OrderCorrelationId = Guid.NewGuid()
        };
        A.CallTo(() => _mediator.Send(A<GetFobtBarcodeHistory>._, A<CancellationToken>._)).Returns<FOBTBarcodeHistory>(null);

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_RecordExistsInHistoryButNotInFobt_ReturnNull()
    {
        // Arrange
        var request = new GetFobtByHistory
        {
            Barcode = "987654321",
            OrderCorrelationId = Guid.NewGuid()
        };
        A.CallTo(() => _mediator.Send(A<GetFobtBarcodeHistory>._, A<CancellationToken>._)).Returns(FobtBarcodeHistoryMock.BuildFobtBarcodeHistory());
        A.CallTo(() => _mediator.Send(A<GetFobtByFobtId>._, A<CancellationToken>._)).Returns<Core.Data.Entities.FOBT>(null);

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_RecordLookupException_ThrowsException()
    {
        var request = new GetFobtByHistory
        {
            Barcode = "987654321",
            OrderCorrelationId = Guid.NewGuid()
        };
        A.CallTo(() => _mediator.Send(A<GetFobtBarcodeHistory>._, A<CancellationToken>._)).Returns(FobtBarcodeHistoryMock.BuildFobtBarcodeHistory());
        A.CallTo(() => _mediator.Send(A<GetFobtByFobtId>._, A<CancellationToken>._)).Throws(new Exception());

        // Act And Assert
        await Assert.ThrowsAsync<Exception>(async () => await _handler.Handle(request, default));
    }
}