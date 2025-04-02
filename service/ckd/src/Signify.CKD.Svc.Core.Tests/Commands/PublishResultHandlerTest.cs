using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka;
using Signify.CKD.Svc.Core.Commands;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.CKD.Svc.Core.Tests.Commands;

public class PublishResultHandlerTests
{
    private readonly IMessageProducer _messageProducer = A.Fake<IMessageProducer>();

    private PublishResultHandler CreateSubject()
        => new PublishResultHandler(A.Dummy<ILogger<PublishResultHandler>>(), _messageProducer);

    [Fact]
    public async Task Handle_HappyPath_Test()
    {
        const int evaluationId = 1;

        var result = new Signify.CKD.Svc.Core.Messages.Result
        {
            EvaluationId = evaluationId
        };

        var request = new PublishResult(result);

        var subject = CreateSubject();

        var response = await subject.Handle(request, default);

        Assert.Equal(Unit.Value, response);

        A.CallTo(() => _messageProducer.Produce(A<string>.That.Matches(key =>
                    key == evaluationId.ToString()),
                A<object>.That.Matches(o => o == result),
                A<CancellationToken>._))
            .MustHaveHappened();
    }
}