using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka;
using Signify.PAD.Svc.Core.Commands;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Commands;

public class BillRequestSentTests
{
    private readonly IMessageProducer _messageProducer = A.Fake<IMessageProducer>();

    private BillRequestSentHandler CreateSubject()
        => new(A.Dummy<ILogger<BillRequestSentHandler>>(), _messageProducer);

    [Fact]
    public async Task Handle_BillRequestSentPublished()
    {
        var billRequestSent = new BillRequestSent
        {
            EvaluationId = 1,
            ProductCode = "PAD"
        };

        var result = await CreateSubject().Handle(billRequestSent, CancellationToken.None);
        
        A.CallTo(() => _messageProducer.Produce(
            A<string>.That.Matches(key => key == "1"), A<BillRequestSent>._,
            A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        Assert.True(result);
    }
}