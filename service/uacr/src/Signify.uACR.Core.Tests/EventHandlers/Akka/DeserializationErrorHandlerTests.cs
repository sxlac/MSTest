using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka;
using Signify.AkkaStreams.Kafka.Notifications;
using Signify.Dps.Observability.Library.Services;
using Signify.uACR.Core.Configs;
using Signify.uACR.Core.EventHandlers.Akka;
using Signify.uACR.Core.Events.Akka.DLQ;
using Signify.uACR.Core.FeatureFlagging;
using Xunit;

namespace Signify.uACR.Core.Tests.EventHandlers.Akka;

public class DeserializationErrorHandlerTests
{
    private readonly IMessageProducer _fakeProducer = A.Fake<IMessageProducer>();
    private readonly IPublishObservability _fakePublishObservability = A.Fake<IPublishObservability>();
    private readonly Captured<string> _capturedKey = A.Captured<string>();
    private readonly Captured<BaseDlqMessage> _capturedRecord = A.Captured<BaseDlqMessage>();
    private readonly Captured<string> _capturedEventType = A.Captured<string>();
    private readonly IFeatureFlags _featureFlags = A.Fake<IFeatureFlags>();
    private readonly KafkaTopics _kafkaTopics = new KafkaTopics(){ Topics = new Dictionary<string, string> { { "CDI Events", "cdi_events" }, { "PDF delivered to client", "pdf_delivery" }, { "Evaluation", "evaluation" }, { "Lab Results", "dps_labresult" }, { "Bill Accepted", "rcm_bill"}, { "RMS Labresult", "dps_rms_labresult"} } };
    private DeserializationErrorHandler CreateSubject()
        => new(A.Dummy<ILogger<DeserializationErrorHandler>>(),_fakeProducer, _featureFlags,  _fakePublishObservability, _kafkaTopics);
    
    [Theory]
    [ClassData(typeof(DlqHandlerTestData))]
    public async Task Handle_DeserializationError(DeserializationError notification, BaseDlqMessage publishedMessage)
    {
        A.CallTo(() => _featureFlags.EnableDlq).Returns(true);
        A.CallTo(() => _fakeProducer.Produce(_capturedKey._, _capturedRecord._, A<CancellationToken>._)).DoesNothing();
        A.CallTo(() => _fakePublishObservability.Publish(_capturedEventType._, A<Dictionary<string, object>>._, true)).DoesNothing();
        
        var subject = CreateSubject();
        await subject.Handle(notification, default);
        _capturedKey.GetLastValue().Should().Be(notification.Key);
        _capturedRecord.GetLastValue().GetType().Should().BeSameAs(publishedMessage.GetType());
        A.CallTo(_fakePublishObservability).MustHaveHappenedOnceExactly();
        _capturedEventType.GetLastValue().Should()
            .Be(Constants.Observability.DeserializationErrors.ErrorPublishedToDlqEvent);
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
        _capturedKey.GetLastValue().Should().NotBeNull();
        _capturedRecord.GetLastValue().GetType().Should().BeSameAs(publishedMessage.GetType());
        A.CallTo(_fakePublishObservability).MustHaveHappenedOnceExactly();
        _capturedEventType.GetLastValue().Should()
            .Be(Constants.Observability.DeserializationErrors.ErrorPublishedToDlqEvent);
    }
    
    [Theory]
    [ClassData(typeof(DlqHandlerInvalidTopicTestData))]
    public async Task Handle_DeserializationError_InvalidTopic(DeserializationError notification, BaseDlqMessage publishedMessage)
    {
        A.CallTo(() => _featureFlags.EnableDlq).Returns(true);
        A.CallTo(() => _fakeProducer.Produce(_capturedKey._, _capturedRecord._, A<CancellationToken>._)).DoesNothing();
        A.CallTo(() => _fakePublishObservability.Publish(_capturedEventType._, A<Dictionary<string, object>>._, true)).DoesNothing();
        
        await Assert.ThrowsAnyAsync<InvalidOperationException>(async () => await CreateSubject().Handle(notification, default));
        A.CallTo(_fakePublishObservability).MustNotHaveHappened();
        A.CallTo(_fakeProducer).MustNotHaveHappened();
    }
    
    [Theory]
    [ClassData(typeof(DlqHandlerTestData))]
    public async Task Handle_DeserializationError_Flag_Disabled(DeserializationError notification, BaseDlqMessage publishedMessage)
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
            yield return new object[] { new DeserializationError("body", "cdi_events", 0, "123456", 0, null), new CdiEventDlqMessage() };
            yield return new object[] { new DeserializationError("body", "pdf_delivery", 0, "78910", 0, null), new PdfDeliveryDlqMessage() };
            yield return new object[] { new DeserializationError("body", "evaluation", 0, "111213", 0, null), new EvaluationDlqMessage() };
            yield return new object[] { new DeserializationError("body", "dps_labresult", 0, "115513", 0, null), new DpsLabResultDlqMessage() };
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    
    private class DlqHandlerTestDataNoKey : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { new DeserializationError("body", "evaluation", 0, null, 0, null), new EvaluationDlqMessage() };
            yield return new object[] { new DeserializationError("body", "rcm_bill", 0, null, 0, null), new RcmBillDlqMessage() };
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    
    private class DlqHandlerInvalidTopicTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { new DeserializationError("body", "cdi_eventss", 0, "123456", 0, null), new CdiEventDlqMessage() };
            yield return new object[] { new DeserializationError("body", "", 0, "78910", 0, null), new PdfDeliveryDlqMessage() };
            yield return new object[] { new DeserializationError("body", "evaluatione", 0, "111213", 0, null), new EvaluationDlqMessage() };
            yield return new object[] { new DeserializationError("body", "dps_labresults", 0, "115513", 0, null), new DpsLabResultDlqMessage() };
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}