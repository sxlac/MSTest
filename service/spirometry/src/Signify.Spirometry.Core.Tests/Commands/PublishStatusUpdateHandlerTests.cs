using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka;
using Signify.Spirometry.Core.Commands;
using Signify.Spirometry.Core.Events.Status;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Commands;

public class PublishStatusUpdateHandlerTests
{
    private readonly IMessageProducer _messageProducer = A.Fake<IMessageProducer>();

    private PublishStatusUpdateHandler CreateSubject()
        => new(A.Dummy<ILogger<PublishStatusUpdateHandler>>(), _messageProducer);

    [Fact]
    public async Task Handle_HappyPath_Tests()
    {
        const int evaluationId = 1;

        var status = new Performed
        {
            EvaluationId = evaluationId
        };

        var request = new PublishStatusUpdate(Guid.Empty, status);

        var subject = CreateSubject();

        await subject.Handle(request, default);

        A.CallTo(() => _messageProducer.Produce(A<string>.That.Matches(key =>
                    key == evaluationId.ToString()),
                A<object>.That.Matches(o => o == status),
                A<CancellationToken>._))
            .MustHaveHappened();
    }
}