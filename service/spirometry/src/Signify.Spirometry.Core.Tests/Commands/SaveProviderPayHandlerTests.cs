using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.Spirometry.Core.Commands;
using Signify.Spirometry.Core.Events;
using SpiroNsb.SagaEvents;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Commands;

public class SaveProviderPayHandlerTests
{
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly FakeApplicationTime _applicationTime = new();

    private SaveProviderPayHandler CreateSubject()
        => new(A.Dummy<ILogger<SaveProviderPayHandler>>(), _mediator, _mapper, _applicationTime);

    [Fact]
    public async Task Handle_Write_To_Database_And_Publish_To_Kafka()
    {
        var subject = CreateSubject();
        var context = new TestableMessageHandlerContext();
        var message = A.Fake<SaveProviderPay>();
        message.EvaluationId = 123456;
        message.EventId = Guid.NewGuid();
        message.ParentEventDateTime = new FakeApplicationTime().UtcNow();
        message.Context = context;
        
        await subject.Handle(message, A.Dummy<CancellationToken>());

        A.CallTo(() => _mediator.Send(A<AddProviderPay>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<ExamStatusEvent>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        Assert.Single(context.SentMessages);
        var providerPayCompleteMessage = context.FindSentMessage<ProviderPaidEvent>();
        providerPayCompleteMessage.EvaluationId = message.EvaluationId;
        providerPayCompleteMessage.CreatedDateTime = _applicationTime.UtcNow();
    }

    [Fact]
    public async Task Handle_Write_To_Database_Throws_Exception()
    {
        var subject = CreateSubject();
        var message = A.Fake<SaveProviderPay>();
        var context = new TestableMessageHandlerContext();
        A.CallTo(() => _mediator.Send(A<AddProviderPay>._, A<CancellationToken>._)).Throws<Exception>();

        await Assert.ThrowsAnyAsync<Exception>(async () => await subject.Handle(message, A.Dummy<CancellationToken>()));

        A.CallTo(() => _mediator.Send(A<AddProviderPay>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<ExamStatusEvent>._, A<CancellationToken>._)).MustNotHaveHappened();
        Assert.Empty(context.SentMessages);
    }

    [Fact]
    public async Task Handle_Publish_To_Kafka_Throws_Exception()
    {
        var subject = CreateSubject();
        var message = A.Fake<SaveProviderPay>();
        var context = new TestableMessageHandlerContext();
        A.CallTo(() => _mediator.Send(A<ExamStatusEvent>._, A<CancellationToken>._)).Throws<Exception>();

        await Assert.ThrowsAnyAsync<Exception>(async () => await subject.Handle(message, A.Dummy<CancellationToken>()));

        A.CallTo(() => _mediator.Send(A<AddProviderPay>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<ExamStatusEvent>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        Assert.Empty(context.SentMessages);
    }
}