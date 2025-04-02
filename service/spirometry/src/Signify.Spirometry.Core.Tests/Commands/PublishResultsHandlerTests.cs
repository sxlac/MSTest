using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka;
using Signify.Spirometry.Core.Commands;
using Signify.Spirometry.Core.Events.Akka;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Commands;

public class PublishResultsHandlerTests
{
    private readonly IMessageProducer _messageProducer = A.Fake<IMessageProducer>();

    private PublishResultsHandler CreateSubject() => new(A.Dummy<ILogger<PublishResultsHandler>>(), _messageProducer);

    [Fact]
    public async Task Handle_HappyPath_Tests()
    {
        const int evaluationId = 1;

        var results = new ResultsReceived
        {
            EvaluationId = evaluationId
        };

        var request = new PublishResults(results, Guid.Empty);

        await CreateSubject().Handle(request, default);

        A.CallTo(() => _messageProducer.Produce(
                A<string>.That.Matches(key => key == evaluationId.ToString()),
                A<object>.That.Matches(o => o == results), A<CancellationToken>._))
            .MustHaveHappened();
    }
}