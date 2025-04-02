using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka;
using Signify.Dps.Observability.Library.Services;
using Signify.PAD.Svc.Core.Commands;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Commands;

public class PadNotPerformedTests
{
    private readonly IMessageProducer _messageProducer = A.Fake<IMessageProducer>();
    private readonly IObservabilityService _observabilityService = A.Fake<IObservabilityService>();

    private PublishPadNotPerformedHandler CreateSubject()
        => new(A.Dummy<ILogger<PublishPadNotPerformedHandler>>(), _messageProducer, _observabilityService);

    [Fact]
    public async Task Handle_PadNotPerformedPublished()
    {
        var performed = new NotPerformed
        {
            EvaluationId = 1,
            ProductCode = "PAD"
        };

        var result = await CreateSubject().Handle(performed, CancellationToken.None);
        
        A.CallTo(() => _messageProducer.Produce(
            A<string>.That.Matches(key => key == "1"), A<NotPerformed>._,
            A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        Assert.True(result);
    }
}