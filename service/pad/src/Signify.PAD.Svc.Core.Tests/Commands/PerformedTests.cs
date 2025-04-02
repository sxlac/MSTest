using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka;
using Signify.Dps.Observability.Library.Services;
using Signify.PAD.Svc.Core.Commands;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Commands;

public class PerformedTests
{
    private readonly IMessageProducer _messageProducer = A.Fake<IMessageProducer>();
    private readonly IObservabilityService _observabilityService = A.Fake<IObservabilityService>();

    private PadPerformedHandler CreateSubject()
        => new(A.Dummy<ILogger<PadPerformedHandler>>(), _messageProducer, _observabilityService);

    [Fact]
    public async Task Handle_PadPerformedPublished()
    {
        var performed = new Performed
        {
            EvaluationId = 1,
            ProductCode = "PAD"
        };

        var result = await CreateSubject().Handle(performed, CancellationToken.None);
        
        A.CallTo(() => _messageProducer.Produce(
            A<string>.That.Matches(key => key == "1"), A<Performed>._,
            A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        Assert.True(result);
    }
}