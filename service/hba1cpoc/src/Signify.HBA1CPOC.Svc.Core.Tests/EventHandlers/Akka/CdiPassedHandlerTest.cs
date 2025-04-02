using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.HBA1CPOC.Messages.Events.Akka;
using Signify.HBA1CPOC.Messages.Events;
using Signify.HBA1CPOC.Svc.Core.EventHandlers.Akka;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.HBA1CPOC.Svc.Core.Tests.EventHandlers.Akka;

public class CdiPassedHandlerTest
{
    private readonly TestableEndpointInstance _endpointInstance;
    private readonly CdiPassedEventHandler _cdiPassedHandler;
    private readonly CDIPassedEvent _cdiPassedEvent;

    public CdiPassedHandlerTest()
    {
        _cdiPassedEvent = new CDIPassedEvent
        {
            RequestId = Guid.NewGuid(),
            EvaluationId = 12345,
            DateTime = DateTimeOffset.UtcNow,
            UserName = "vastest1",
            ApplicationId = "Signify.Evaluation.Service"
        };
        _endpointInstance = new TestableEndpointInstance();
        _cdiPassedHandler = new CdiPassedEventHandler(A.Dummy<ILogger<CdiPassedEventHandler>>(), _endpointInstance);
    }

    [Theory]
    [InlineData("CKD", 0)]
    [InlineData("HBA1CPOC", 1)]
    public async Task EvaluationFinalizedHandler_WhenProductCodeCorrect(string productCode, int valid)
    {
        _cdiPassedEvent.Products = new List<DpsProduct>
            { new DpsProduct(12345, productCode), new DpsProduct(12345, "PAD") };

        await _cdiPassedHandler.Handle(_cdiPassedEvent, CancellationToken.None);
        _endpointInstance.SentMessages.Length.Should().Be(valid);
    }
    
    [Fact]
    public async Task EvaluationFinalizedHandler_WhenProductCodeIsMissing()
    {
        await _cdiPassedHandler.Handle(_cdiPassedEvent, CancellationToken.None);
        _endpointInstance.SentMessages.Length.Should().Be(0);
    }
}