using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Commands;

public class PublishAoeResultTests
{
    private readonly PublishAoeResultHandler _handler;
    private readonly IMessageProducer _messageProducer = A.Fake<IMessageProducer>();

    public PublishAoeResultTests()
    {
        var logger = A.Fake<ILogger<PublishAoeResultHandler>>();
        _handler = new PublishAoeResultHandler(logger, _messageProducer);
    }

    [Fact]
    public async Task Handle_PublishKafkaMessage_ExecutesWithoutException()
    {
        // Arrange
        var aoeResult = new AoeResult
        {
            EvaluationId = 1,
            ReceivedDate = DateTime.UtcNow,
            ClinicalSupport = []
        };

        var request = new PublishAoeResult(aoeResult);

        // Act
        await _handler.Handle(request, default);

        // Assert
        A.CallTo(() => _messageProducer.Produce(A<string>._, A<object>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }
}
