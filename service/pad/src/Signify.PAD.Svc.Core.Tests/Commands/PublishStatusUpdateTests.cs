using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Events.Status;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Commands;

public class PublishStatusUpdateTests
{
    private readonly IMessageProducer _messageProducer = A.Fake<IMessageProducer>();

    private PublishStatusUpdateHandler CreateSubject()
        => new(A.Dummy<ILogger<PublishStatusUpdateHandler>>(), _messageProducer);

    [Fact]
    public async Task Handle_Test()
    {
        // Arrange
        const int evaluationId = 1;

        var status = new BillRequestNotSent
        {
            EvaluationId = evaluationId
        };

        var request = new PublishStatusUpdate(Guid.Empty, status);

        // Act
        await CreateSubject().Handle(request, default);

        // Assert
        A.CallTo(() => _messageProducer.Produce(A<string>.That.Matches(key =>
                    key == "1"),
                A<object>.That.Matches(o => o == status),
                A<CancellationToken>._))
            .MustHaveHappened();
    }
}