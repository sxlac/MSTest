
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka;
using Signify.AkkaStreams.Kafka.Notifications;
using Signify.HBA1CPOC.Svc.Core.Configs.Kafka;
using Signify.HBA1CPOC.Svc.Core.EventHandlers;
using Signify.HBA1CPOC.Svc.Core.Events;
using Signify.HBA1CPOC.Svc.Core.Infrastructure.Observability;
using Xunit;

namespace Signify.HBA1CPOC.Svc.Core.Tests.EventHandlers;


public class DeserializationErrorHandlerTest
{
    private readonly IMessageProducer _fakeProducer = A.Fake<IMessageProducer>();
    private readonly IPublishObservability _fakePublishObservability = A.Fake<IPublishObservability>();
    private readonly Captured<string> _capturedKey = A.Captured<string>();
    private readonly Captured<BaseDlqMessage> _capturedRecord = A.Captured<BaseDlqMessage>();
    private readonly Captured<ObservabilityEvent> _capturedEventType = A.Captured<ObservabilityEvent>();
    
    private DeserializationErrorHandler CreateSubject()
        => new(A.Dummy<ILogger<DeserializationErrorHandler>>(),_fakeProducer, new KafkaDlqConfig { IsDlqEnabled = true }, _fakePublishObservability);

    [Theory]
    [ClassData(typeof(DlqHandlerTestData))]
    public async Task Handle_DeserializationError(DeserializationError notification, BaseDlqMessage publishedMessage)
    {
        A.CallTo(() => _fakeProducer.Produce(_capturedKey._, _capturedRecord._, A<CancellationToken>._)).DoesNothing();
        A.CallTo(() => _fakePublishObservability.RegisterEvent(_capturedEventType._, true)).DoesNothing();
        
        var subject = CreateSubject();
        await subject.Handle(notification, default);
        _capturedKey.GetLastValue().Should().Be(notification.Key);
        _capturedRecord.GetLastValue().GetType().Should().BeSameAs(publishedMessage.GetType());
        A.CallTo(_fakePublishObservability).MustHaveHappenedOnceExactly();
        _capturedEventType.GetLastValue().EventType.Should()
            .Be(Constants.Observability.DeserializationErrors.ErrorPublishedToDlqEvent);
    }
    
    [Theory]
    [ClassData(typeof(DlqHandlerTestDataNoKey))]
    public async Task Handle_DeserializationError_NoKeyPresent(DeserializationError notification, BaseDlqMessage publishedMessage)
    {
        A.CallTo(() => _fakeProducer.Produce(_capturedKey._, _capturedRecord._, A<CancellationToken>._)).DoesNothing();
        A.CallTo(() => _fakePublishObservability.RegisterEvent(_capturedEventType._, true)).DoesNothing();
        
        var subject = CreateSubject();
        await subject.Handle(notification, default);
        _capturedKey.GetLastValue().Should().NotBeNull();
        _capturedRecord.GetLastValue().GetType().Should().BeSameAs(publishedMessage.GetType());
        A.CallTo(_fakePublishObservability).MustHaveHappenedOnceExactly();
        _capturedEventType.GetLastValue().EventType.Should()
            .Be(Constants.Observability.DeserializationErrors.ErrorPublishedToDlqEvent);
    }
    
    private class DlqHandlerTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { new DeserializationError("body", "cdi_events", 0, "123456", 0, null), new CdiEventDlqMessage() };
            yield return new object[] { new DeserializationError("body", "pdf_delivery", 0, "78910", 0, null), new PdfDeliveryDlqMessage() };
            yield return new object[] { new DeserializationError("body", "evaluation", 0, "111213", 0, null), new EvaluationDlqMessage() };
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
}