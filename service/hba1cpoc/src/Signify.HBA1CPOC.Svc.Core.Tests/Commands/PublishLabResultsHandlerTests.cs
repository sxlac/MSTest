using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka;
using Signify.HBA1CPOC.Messages.Events.Akka;
using Signify.HBA1CPOC.Svc.Core.Commands;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.HBA1CPOC.Svc.Core.Tests.Commands;

public class PublishLabResultsTests
{
    private readonly IMessageProducer _producer = A.Fake<IMessageProducer>();

    private PublishLabResultsHandler CreateSubject() => new(A.Dummy<ILogger<PublishLabResultsHandler>>(), _producer);

    [Fact]
    public async Task Handle_HappyPath_Test()
    {
        const long evaluationId = 1;

        var results = new ResultsReceived
        {
            EvaluationId = evaluationId
        };

        var subject = CreateSubject();

        var actual = await subject.Handle(new PublishLabResults(results), default);

        Assert.Equal(Unit.Value, actual);
        A.CallTo(() => _producer.Produce(A<string>.That.Matches(s =>
                    s == evaluationId.ToString()),
                A<object>.That.Matches(o => o == results), A<CancellationToken>._))
            .MustHaveHappened();
    }
}