using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.uACR.Core.EventHandlers.Akka;
using Signify.uACR.Core.Events;
using Signify.uACR.Core.FeatureFlagging;
using Xunit;

namespace Signify.uACR.Core.Tests.EventHandlers.Akka;

public class LabResultHandlerTests
{
    private readonly TestableEndpointInstance _messageSession = new();
    private readonly IFeatureFlags _featureFlags = A.Fake<IFeatureFlags>();
    private readonly FakeApplicationTime _fakeApplicationTime = new();

    private LabResultHandler CreateSubject()
        => new(A.Dummy<ILogger<LabResultHandler>>(),
            _messageSession,
            _fakeApplicationTime,
            _featureFlags);

    [Fact]
    public async Task Handle_LabResult_CallToNsbOccured()
    {
        A.CallTo(() => _featureFlags.EnableLabResultIngestion).Returns(true);

        var e = new KedUacrLabResult();
        var subject = CreateSubject();
        await subject.Handle(e, default);

        _messageSession.FindSentMessage<KedUacrLabResult>();

        A.CallTo(() => _featureFlags.EnableLabResultIngestion).MustHaveHappened(1, Times.Exactly);
        Assert.Single(_messageSession.SentMessages);
    }
}