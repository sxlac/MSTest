using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.DEE.Svc.Core.EventHandlers;
using Signify.DEE.Svc.Core.Events;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Signify.Dps.Observability.Library.Services;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.EventHandlers;

public class PdfDeliveredHandlerTest
{
    private readonly PdfDeliveredHandler _pdfEventHandler;
    private readonly TestableMessageSession _endpointInstance;

    public PdfDeliveredHandlerTest()
    {
        _endpointInstance = new TestableMessageSession();
        _pdfEventHandler = new PdfDeliveredHandler(A.Dummy<ILogger<PdfDeliveredHandler>>(), 
            A.Dummy<IMapper>(), 
            _endpointInstance, 
            A.Fake<IPublishObservability>());
    }

    [Fact]
    public async Task Handle_WithNonDEEProduct_IsIgnored()
    {
        var @event = new PdfDeliveredToClient
        {
            ProductCodes = new List<string> { "Spirometry" }
        };

        await _pdfEventHandler.Handle(@event, CancellationToken.None);

        _endpointInstance.SentMessages.Length.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithDEEProduct_IsProcessed()
    {
        var @event = new PdfDeliveredToClient
        {
            ProductCodes = new List<string> { "DEE" }
        };

        await _pdfEventHandler.Handle(@event, CancellationToken.None);

        _endpointInstance.SentMessages.Length.Should().Be(1);
    }
}