using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using Signify.eGFR.Core.Constants;
using Signify.eGFR.Core.EventHandlers.Akka;
using Signify.eGFR.Core.Events;
using Signify.eGFR.Core.FeatureFlagging;
using Signify.eGFR.Core.Filters;
using Xunit;

namespace Signify.eGFR.Core.Tests.EventHandlers.Akka;

public class InternalLabResultHandlerTests
{
    private readonly TestableEndpointInstance _messageSession = new();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();
    private readonly IFeatureFlags _featureFlags = A.Fake<IFeatureFlags>();
    private readonly IProductFilter _productFilter = A.Fake<IProductFilter>();
    private readonly ILogger<InternalLabResultHandler> _logger = A.Dummy<ILogger<InternalLabResultHandler>>();

    private InternalLabResultHandler CreateSubject()
    {
        return new InternalLabResultHandler(_logger, _productFilter, _messageSession, _publishObservability, _featureFlags);
    }

    [Fact]
    public async Task Handle_LabResult_CallToNsbOccured_HappyPath()
    {
        var expectedLabResultId = 32;
        var expectedProductCodes = new string[]{ "EGFR", "UACR" };
        var e = new LabResultReceivedEvent()
        {
            LabResultId = 32,
            ProductCodes = new string[]{ "EGFR", "UACR" }
        };
        
        A.CallTo(() => _featureFlags.EnableInternalLabResultIngestion).Returns(true);      
        A.CallTo(() => _productFilter.ShouldProcess(e.ProductCodes)).Returns(true);
        
        var subject = CreateSubject();
        await subject.Handle(e, default);

        var sentMessage = _messageSession.FindSentMessage<LabResultReceivedEvent>();
        
        A.CallTo(() => _featureFlags.EnableInternalLabResultIngestion).MustHaveHappened(1, Times.Exactly);
        A.CallTo(() => _publishObservability.RegisterEvent(
            A<ObservabilityEvent>.That.Matches(e => e.EventType == Observability.LabResult.InternalLabResultReceived), true)).MustHaveHappenedOnceExactly();
        
        Assert.NotNull(sentMessage);
        Assert.Equal(expectedLabResultId, sentMessage.LabResultId);
        Assert.Equal(expectedProductCodes, sentMessage.ProductCodes);
        Assert.Single(_messageSession.SentMessages);
        
    }
    
    [Fact]
    public async Task Handle_LabResult_FlagDisabled()
    {
        var e = new LabResultReceivedEvent();
        
        A.CallTo(() => _featureFlags.EnableInternalLabResultIngestion).Returns(false);
        
        var subject = CreateSubject();
        await subject.Handle(e, default);
        
        var sentMessage = _messageSession.FindSentMessage<LabResultReceivedEvent>();
        
        A.CallTo(() => _productFilter.ShouldProcess(A<string>._)).MustNotHaveHappened();
        A.CallTo(() => _publishObservability.RegisterEvent(
            A<ObservabilityEvent>.That.Matches(e => e.EventType == Observability.LabResult.InternalLabResultReceived), true)).MustNotHaveHappened();
        A.CallTo(() => _publishObservability.Commit()).MustNotHaveHappened();
        Assert.Null(sentMessage);
        Assert.Empty(_messageSession.SentMessages);
    }
    
    [Fact]
    public async Task Handle_LabResult_Invalid_ProductCode()
    {
        var e = new LabResultReceivedEvent()
        {
            LabResultId = 32,
            ProductCodes = new string[]{ "CECG" }
        };
        
        A.CallTo(() => _featureFlags.EnableInternalLabResultIngestion).Returns(true);
        A.CallTo(() => _productFilter.ShouldProcess(e.ProductCodes)).Returns(false);
        
        var subject = CreateSubject();
        await subject.Handle(e, default);

        var sentMessage = _messageSession.FindSentMessage<LabResultReceivedEvent>();

        A.CallTo(() => _featureFlags.EnableInternalLabResultIngestion).MustHaveHappened(1, Times.Exactly);
        A.CallTo(() => _productFilter.ShouldProcess(e.ProductCodes)).MustHaveHappened();
        A.CallTo(() => _publishObservability.RegisterEvent(
            A<ObservabilityEvent>.That.Matches(e => e.EventType == Observability.LabResult.InternalLabResultReceived), true)).MustNotHaveHappened();
        A.CallTo(() => _publishObservability.Commit()).MustNotHaveHappened();
        Assert.Null(sentMessage);
        Assert.Empty(_messageSession.SentMessages);
        
    }
}