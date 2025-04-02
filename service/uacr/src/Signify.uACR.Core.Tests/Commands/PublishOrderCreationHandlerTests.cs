using System;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka;
using Signify.uACR.Core.Commands;
using Signify.uACR.Core.Events.Akka;
using Xunit;

namespace Signify.uACR.Core.Tests.Commands;

public class PublishOrderCreationHandlerTests
{
    private readonly IMessageProducer _messageProducer = A.Fake<IMessageProducer>();

    private PublishOrderCreationHandler CreateSubject()
        => new(A.Dummy<ILogger<PublishOrderCreationHandler>>(), _messageProducer);

    [Fact]
    public async Task Handle_HappyPath_Tests()
    {
        const long evaluationId = 1;

        var results = new OrderCreationEvent()
        {
            EvaluationId = evaluationId
        };

        var request = new PublishOrderCreation(results, Guid.Empty);

        await CreateSubject().Handle(request, default);

        A.CallTo(() => _messageProducer.Produce(
                A<string>.That.Matches(key => key == evaluationId.ToString()),
                A<object>.That.Matches(o => o == results), A<CancellationToken>._))
            .MustHaveHappened();
    }
}