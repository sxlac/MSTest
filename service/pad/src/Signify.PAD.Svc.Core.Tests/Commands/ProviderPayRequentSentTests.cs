using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka;
using Signify.Dps.Observability.Library.Services;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Constants;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Commands;

public class ProviderPayRequentSentTests
{
    private readonly IMessageProducer _messageProducer = A.Fake<IMessageProducer>();
    
    private readonly IObservabilityService _observabilityService = A.Fake<IObservabilityService>();
    
    // Component under test
    private ProviderPaySentHandler CreateSubject()
        => new(A.Dummy<ILogger<ProviderPaySentHandler>>(), 
            _messageProducer,
            _observabilityService);
    
    [Fact]
    public async Task Handle_Provider_Pay_Request_Sent_Test()
    {
        // Arrange
        const int evaluationId = 1;
        const string providerPayProductCode = "PAD";
        const string paymentId = "123-456-789";
        var dateDelivered = DateTime.UtcNow;

        var providerPayRequestSent = new ProviderPayRequestSent
        {
            EvaluationId = evaluationId,
            ProviderPayProductCode = providerPayProductCode,
            PaymentId = paymentId,
            PdfDeliveryDate = dateDelivered
        };
        
        // Act
        await CreateSubject().Handle(providerPayRequestSent, default);

        // Assert
        A.CallTo(() => _messageProducer.Produce(
            A<string>.That.Matches(key => key == "1"),
            A<object>.That.Matches(o => o == providerPayRequestSent),
            A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _observabilityService.AddEvent(Observability.ProviderPay.PayableCdiEvents, A<Dictionary<string, object>>._))
            .MustHaveHappenedOnceExactly();
    }
}