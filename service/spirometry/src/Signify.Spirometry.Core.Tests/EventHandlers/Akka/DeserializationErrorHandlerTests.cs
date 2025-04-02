using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka;
using Signify.AkkaStreams.Kafka.Notifications;
using Signify.Dps.Observability.Library.Services;
using Signify.Spirometry.Core.EventHandlers.Akka;
using Signify.Spirometry.Core.Events.Akka.DLQ;
using Signify.Spirometry.Core.FeatureFlagging;
using Xunit;

namespace Signify.Spirometry.Core.Tests.EventHandlers.Akka;

public class DeserializationErrorHandlerTests
{
    private readonly IMessageProducer _fakeProducer = A.Fake<IMessageProducer>();
    private readonly IPublishObservability _fakePublishObservability = A.Fake<IPublishObservability>();
    private readonly Captured<string> _capturedKey = A.Captured<string>();
    private readonly Captured<BaseDlqMessage> _capturedRecord = A.Captured<BaseDlqMessage>();
    private readonly Captured<string> _capturedEventType = A.Captured<string>();
    private readonly IFeatureFlags _featureFlags = A.Fake<IFeatureFlags>();
    private DeserializationErrorHandler CreateSubject()
        => new(A.Dummy<ILogger<DeserializationErrorHandler>>(),_fakeProducer, _featureFlags,  _fakePublishObservability);
    
    [Theory]
    [ClassData(typeof(DlqHandlerTestData))]
    public async Task Handle_DeserializationError(DeserializationError notification, BaseDlqMessage publishedMessage)
    {
        A.CallTo(() => _featureFlags.EnableDlq).Returns(true);
        A.CallTo(() => _fakeProducer.Produce(_capturedKey._, _capturedRecord._, A<CancellationToken>._)).DoesNothing();
        A.CallTo(() => _fakePublishObservability.Publish(_capturedEventType._, A<Dictionary<string, object>>._, true)).DoesNothing();
        
        var subject = CreateSubject();
        await subject.Handle(notification, default);
        Assert.Equal(_capturedKey.GetLastValue(), notification.Key);
        Assert.Equal(_capturedRecord.GetLastValue().GetType(), publishedMessage.GetType());
        A.CallTo(_fakePublishObservability).MustHaveHappenedOnceExactly();
        Assert.Equal(Constants.Observability.DeserializationErrors.ErrorPublishedToDlqEvent, _capturedEventType.GetLastValue());
    }
    
    [Theory]
    [ClassData(typeof(DlqHandlerTestDataNoKey))]
    public async Task Handle_DeserializationError_NoKeyPresent(DeserializationError notification, BaseDlqMessage publishedMessage)
    {
        A.CallTo(() => _featureFlags.EnableDlq).Returns(true);
        A.CallTo(() => _fakeProducer.Produce(_capturedKey._, _capturedRecord._, A<CancellationToken>._)).DoesNothing();
        A.CallTo(() => _fakePublishObservability.Publish(_capturedEventType._, A<Dictionary<string, object>>._, true)).DoesNothing();
        
        var subject = CreateSubject();
        await subject.Handle(notification, default);
        Assert.NotNull(_capturedKey.GetLastValue());
        Assert.Equal(_capturedRecord.GetLastValue().GetType(), publishedMessage.GetType());
        A.CallTo(_fakePublishObservability).MustHaveHappenedOnceExactly();
        Assert.Equal(Constants.Observability.DeserializationErrors.ErrorPublishedToDlqEvent, _capturedEventType.GetLastValue());
    }
    
    [Theory]
    [ClassData(typeof(DlqHandlerInvalidTopicTestData))]
    public async Task Handle_DeserializationError_InvalidTopic(DeserializationError notification)
    {
        A.CallTo(() => _featureFlags.EnableDlq).Returns(true);
        A.CallTo(() => _fakeProducer.Produce(_capturedKey._, _capturedRecord._, A<CancellationToken>._)).DoesNothing();
        A.CallTo(() => _fakePublishObservability.Publish(_capturedEventType._, A<Dictionary<string, object>>._, true)).DoesNothing();
        
        await Assert.ThrowsAnyAsync<InvalidOperationException>(async () => await CreateSubject().Handle(notification, default));
        A.CallTo(_fakePublishObservability).MustNotHaveHappened();
        A.CallTo(_fakeProducer).MustNotHaveHappened();
    }
    
    [Theory]
    [ClassData(typeof(DlqHandlerNotPublishedTestData))]
    public async Task Handle_DeserializationError_Flag_Disabled(DeserializationError notification)
    {
        A.CallTo(() => _featureFlags.EnableDlq).Returns(false);
        A.CallTo(() => _fakeProducer.Produce(_capturedKey._, _capturedRecord._, A<CancellationToken>._)).DoesNothing();
        A.CallTo(() => _fakePublishObservability.Publish(_capturedEventType._, A<Dictionary<string, object>>._, true)).DoesNothing();
        
        var subject = CreateSubject();
        await subject.Handle(notification, default);
        A.CallTo(_fakePublishObservability).MustNotHaveHappened();
        A.CallTo(_fakeProducer).MustNotHaveHappened();
    }
    
    private class DlqHandlerTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return [new DeserializationError("body", "cdi_events", 0, "123456", 0, null), new CdiEventDlqMessage()];
            yield return [new DeserializationError("body", "cdi_holds", 0, "78910", 0, null), new CdiHoldsDlqMessage()];
            yield return [new DeserializationError("body", "pdfdelivery", 0, "78910", 0, null), new PdfDeliveryDlqMessage()];
            yield return [new DeserializationError("body", "overread_spirometry", 0, "78910", 0, null), new OverreadDlqMessage()];
            yield return [new DeserializationError("body", "evaluation", 0, "111213", 0, null), new EvaluationDlqMessage()];
            yield return [new DeserializationError("body", "rcm_bill", 0, "515151", 0, null), new RcmBillDlqMessage()];
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private class DlqHandlerNotPublishedTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return [new DeserializationError("body", "cdi_events", 0, "123456", 0, null)];
            yield return [new DeserializationError("body", "pdfdelivery", 0, "78910", 0, null)];
            yield return [new DeserializationError("body", "evaluation", 0, "111213", 0, null)];
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    
    private class DlqHandlerTestDataNoKey : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return [new DeserializationError("body", "evaluation", 0, null, 0, null), new EvaluationDlqMessage()];
            yield return [new DeserializationError("body", "rcm_bill", 0, null, 0, null), new RcmBillDlqMessage()];
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    
    private class DlqHandlerInvalidTopicTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return [new DeserializationError("body", "cdi_eventss", 0, "123456", 0, null)];
            yield return [new DeserializationError("body", "", 0, "78910", 0, null)];
            yield return [new DeserializationError("body", "evaluatione", 0, "111213", 0, null)];
            yield return [new DeserializationError("body", "rcm_biller", 0, "115513", 0, null)];
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}