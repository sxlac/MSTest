using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.eGFR.Core.EventHandlers.Akka;
using Signify.eGFR.Core.Events;
using Xunit;

namespace Signify.eGFR.Core.Tests.EventHandlers.Akka;

public class QuestLabResultHandlerTests
{
    private readonly TestableEndpointInstance _messageSession = new();
    private readonly FakeApplicationTime _applicationTime = new();

    private QuestLabResultHandler CreateSubject()
    {
        return new QuestLabResultHandler(A.Dummy<ILogger<QuestLabResultHandler>>(), _messageSession, _applicationTime);
    }

    [Fact]
    public async Task Handle_LabResult_CallToNsbOccured()
    {
        var e = new EgfrLabResult();
        var subject = CreateSubject();
        await subject.Handle(e, default);

        var sentMessage = _messageSession.FindSentMessage<EgfrLabResult>();
        Assert.Single(_messageSession.SentMessages);
        Assert.Equal(_applicationTime.UtcNow(), sentMessage.ReceivedByEgfrDateTime);
    }
}