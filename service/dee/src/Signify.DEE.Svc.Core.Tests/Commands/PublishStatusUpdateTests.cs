using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka;
using Signify.DEE.Messages.Status;
using Signify.DEE.Svc.Core.Commands;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Commands;

public class PublishStatusUpdateTests
{
    private readonly IMessageProducer _messageProducer = A.Fake<IMessageProducer>();

    private PublishStatusUpdateHandler CreateSubject()
        => new(A.Dummy<ILogger<PublishStatusUpdateHandler>>(), _messageProducer);

    [Fact]
    public async Task Handle_HappyPath_Tests()
    {
        const long evaluationId = 1;

        var status = new Performed
        {
            EvaluationId = evaluationId
        };

        var request = new PublishStatusUpdate(status);

        var subject = CreateSubject();

        var result = await subject.Handle(request, default);

        Assert.Equal(Unit.Value, result);

        A.CallTo(() => _messageProducer.Produce(A<string>.That.Matches(key =>
                    key == evaluationId.ToString()),
                A<object>.That.Matches(o => o == status),
                A<CancellationToken>._))
            .MustHaveHappened();
    }
}