using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NsbEventHandlers;
using NServiceBus.Testing;
using Signify.Dps.Observability.Library.Services;
using Signify.Spirometry.Core.Commands;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Events;
using SpiroNsb.SagaEvents;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.Spirometry.Core.Tests.EventHandlers.Nsb;

public class CdiFailedEventHandlerTests
{
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly FakeApplicationTime _applicationTime = new();
    private readonly FakeTransactionSupplier _transactionSupplier = new();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();

    private CdiFailedEventHandler CreateSubject()
    {
        return new CdiFailedEventHandler(A.Dummy<ILogger<CdiFailedEventHandler>>(), _mediator, _mapper, _applicationTime,
            _transactionSupplier, _publishObservability);
    }

    [Fact]
    public async Task Handle_When_DbQuery_Throws()
    {
        A.CallTo(() => _mediator.Send(A<AddCdiEventForPayment>._, A<CancellationToken>._)).Throws<Exception>();
        var context = new TestableMessageHandlerContext();
        var request = A.Fake<CDIFailedEvent>();

        await Assert.ThrowsAnyAsync<Exception>(async () => await CreateSubject().Handle(request, context));

        A.CallTo(() => _mapper.Map<CdiEventForPayment>(request)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mapper.Map<CdiEventForPaymentReceived>(A<CdiEventForPayment>._)).MustNotHaveHappened();
        Assert.Empty(context.SentMessages);
        _transactionSupplier.AssertRollback();
        A.CallTo(()=>_publishObservability.Commit()).MustNotHaveHappened();
    }

    [Fact]
    public async Task Handle_When_CdiEvent_Already_Exist()
    {
        var response = A.Fake<AddCdiEventForPaymentResponse>();
        response.IsNew = false;
        A.CallTo(() => _mediator.Send(A<AddCdiEventForPayment>._, A<CancellationToken>._)).Returns(response);
        var context = new TestableMessageHandlerContext();
        var request = A.Fake<CDIFailedEvent>();

        await CreateSubject().Handle(request, context);

        A.CallTo(() => _mapper.Map<CdiEventForPayment>(request)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mapper.Map<CdiEventForPaymentReceived>(A<CdiEventForPayment>._)).MustNotHaveHappened();
        Assert.Empty(context.SentMessages);
        _transactionSupplier.AssertCommit();
    }

    [Fact]
    public async Task Handle_When_CdiEvent_DoesNot_Exist()
    {
        var response = A.Fake<AddCdiEventForPaymentResponse>();
        response.IsNew = true;
        A.CallTo(() => _mediator.Send(A<AddCdiEventForPayment>._, A<CancellationToken>._)).Returns(response);
        var context = new TestableMessageHandlerContext();
        long evaluationId = 123456;
        var request = A.Fake<CDIFailedEvent>();
        request.EvaluationId = evaluationId;

        await CreateSubject().Handle(request, context);

        A.CallTo(() => _mapper.Map<CdiEventForPayment>(request)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mapper.Map<CdiEventForPaymentReceived>(A<CdiEventForPayment>._)).MustHaveHappenedOnceExactly();
        Assert.Single(context.SentMessages);
        var paymentEvent = context.FindSentMessage<CdiEventForPaymentReceived>();
        paymentEvent.CreatedDateTime = _applicationTime.UtcNow();
        Assert.NotNull(paymentEvent);
        _transactionSupplier.AssertCommit();
    }
}