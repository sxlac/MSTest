using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.eGFR.Core.EventHandlers.Akka;
using Signify.eGFR.Core.Events;
using Signify.eGFR.Core.FeatureFlagging;
using Xunit;

namespace Signify.eGFR.Core.Tests.EventHandlers.Akka;

public class KedLabResultHandlerTests
{
    private readonly TestableEndpointInstance _messageSession = new();
    private readonly FakeApplicationTime _applicationTime = new();
    private readonly IFeatureFlags _featureFlags = A.Fake<IFeatureFlags>();

    private KedLabResultHandler CreateSubject()
    {
        return new KedLabResultHandler(A.Dummy<ILogger<KedLabResultHandler>>(), _messageSession, _applicationTime,
            _featureFlags);
    }

    [Fact]
    public async Task Handle_LabResult_CallToNsbOccured()
    {
        A.CallTo(() => _featureFlags.EnableKedLabResultIngestion).Returns(true);

        var e = new KedEgfrLabResult();
        var subject = CreateSubject();
        await subject.Handle(e, default);

        var sentMessage = _messageSession.FindSentMessage<KedEgfrLabResult>();

        A.CallTo(() => _featureFlags.EnableKedLabResultIngestion).MustHaveHappened(1, Times.Exactly);
        Assert.Single(_messageSession.SentMessages);
        Assert.Equal(_applicationTime.UtcNow(), sentMessage.ReceivedByEgfrDateTime);
    }
}