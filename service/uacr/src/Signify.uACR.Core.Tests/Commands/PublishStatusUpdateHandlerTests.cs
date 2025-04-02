using System;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka;
using Signify.uACR.Core.Commands;
using Signify.uACR.Core.Events.Status;
using Signify.uACR.Core.Infrastructure;
using Xunit;

namespace Signify.uACR.Core.Tests.Commands;

public class PublishStatusUpdateHandlerTests
{
    private readonly IMessageProducer _messageProducer = A.Fake<IMessageProducer>();
    private readonly IApplicationTime _applicationTime = A.Fake<IApplicationTime>();

    private PublishStatusUpdateHandler CreateSubject()
        => new(A.Dummy<ILogger<PublishStatusUpdateHandler>>(), _messageProducer);

    [Fact]
    public async Task Handle_PublishToKafka()
    {
        const int evaluationId = 1;
        var performed = A.Fake<Performed>();
        performed.EvaluationId = evaluationId;
        performed.MemberPlanId = 12345;
        performed.ProviderId = 5432;
        performed.CreatedDate = _applicationTime.UtcNow();
        performed.ReceivedDate = _applicationTime.UtcNow();
        performed.Barcode = "12345";

        var request = new PublishStatusUpdate(A.Dummy<Guid>(), performed);
        var subject = CreateSubject();

        await subject.Handle(request, default);

        A.CallTo(() => _messageProducer.Produce(
                A<string>.That.Matches(key => key == evaluationId.ToString()),
                A<object>.That.Matches(o => o == performed),
                A<CancellationToken>._))
            .MustHaveHappened();
    }
}