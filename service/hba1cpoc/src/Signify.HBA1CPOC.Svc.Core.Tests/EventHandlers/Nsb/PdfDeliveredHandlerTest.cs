using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.HBA1CPOC.Messages.Events;
using Signify.HBA1CPOC.Svc.Core.EventHandlers;
using Signify.HBA1CPOC.Svc.Core.Infrastructure.Observability;
using Xunit;

namespace Signify.HBA1CPOC.Svc.Core.Tests.EventHandlers.Nsb;

public class PdfDeliveredHandlerTest
{
    private readonly TestableMessageSession _endpointInstance = new();
    private readonly PdfDeliveredHandler _pdfDeliveredHandler;
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();
    public PdfDeliveredHandlerTest()
    {
        _pdfDeliveredHandler = new PdfDeliveredHandler(A.Dummy<ILogger<PdfDeliveredHandler>>(), _endpointInstance, _publishObservability);
    }

    [Fact]
    public async Task Handle_WithDifferentProductCode_DoesNothing()
    {
        // Arrange
        var @event = new PdfDeliveredToClient
        {
            BatchId = 678,
            BatchName = "c0b2f2d9-d39b-4293-ab88-532b5412dc4f",
            CreatedDateTime = DateTime.UtcNow,
            DeliveryDateTime = DateTime.UtcNow,
            EvaluationId = 324357,
            EventId = new Guid("c0b2f2d9-d39b-4293-ab88-532b5412dc4f"),
            ProductCodes = new List<string> { "PAD" }
        };

        // Act
        await _pdfDeliveredHandler.Handle(@event, CancellationToken.None);

        // Assert
        _endpointInstance.SentMessages.Length.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithCorrespondingProductCode_SendsMessage()
    {
        // Arrange
        var @event = new PdfDeliveredToClient
        {
            BatchId = 678,
            BatchName = "c0b2f2d9-d39b-4293-ab88-532b5412dc4f",
            CreatedDateTime = DateTime.UtcNow,
            DeliveryDateTime = DateTime.UtcNow,
            EvaluationId = 324357,
            EventId = new Guid("c0b2f2d9-d39b-4293-ab88-532b5412dc4f"),
            ProductCodes = new List<string> { Constants.ApplicationConstants.ProductCode }
        };

        // Act
        await _pdfDeliveredHandler.Handle(@event, CancellationToken.None);

        // Assert
        _endpointInstance.SentMessages.Length.Should().Be(1);
    }
}