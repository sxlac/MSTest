using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Events;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Commands;

public class PublishResultsTests
{
    private readonly IMessageProducer _messageProducer = A.Fake<IMessageProducer>();
    
    // Component under test
    private PublishResultsHandler CreateSubject()
        => new(A.Dummy<ILogger<PublishResultsHandler>>(), 
            _messageProducer);

    [Fact]
    public async Task Handle_ResultsPublished()
    {
        var resultsReceived = new ResultsReceived
        {
            EvaluationId = 1
        };
        var request = new PublishResults(resultsReceived);

        var result = await CreateSubject().Handle(request, CancellationToken.None);
        
        A.CallTo(() => _messageProducer.Produce(
            A<string>.That.Matches(key => key == "1"), A<ResultsReceived>._,
            A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        Assert.True(result);
    }
}