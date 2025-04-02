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
using Signify.eGFR.Core.Configs.Kafka;
using Signify.eGFR.Core.EventHandlers.Akka;
using Signify.eGFR.Core.Events.Akka.DLQ;
using Xunit;

namespace Signify.eGFR.Core.Tests.EventHandlers.Akka;

public class DeserializationErrorHandlerTests
{
    private readonly IMessageProducer _fakeProducer = A.Fake<IMessageProducer>();
    private readonly IPublishObservability _fakePublishObservability = A.Fake<IPublishObservability>();
    private readonly Captured<string> _capturedKey = A.Captured<string>();
    private readonly Captured<BaseDlqMessage> _capturedRecord = A.Captured<BaseDlqMessage>();
    private readonly Captured<string> _capturedEventType = A.Captured<string>();

    private DeserializationErrorHandler CreateSubject()
        => new(A.Dummy<ILogger<DeserializationErrorHandler>>(),_fakeProducer, new KafkaDlqConfig { IsDlqEnabled = true },  _fakePublishObservability);

    [Theory]
    [ClassData(typeof(DlqHandlerTestData))]
    public async Task Handle_DeserializationError(DeserializationError notification, BaseDlqMessage publishedMessage)
    {
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
}