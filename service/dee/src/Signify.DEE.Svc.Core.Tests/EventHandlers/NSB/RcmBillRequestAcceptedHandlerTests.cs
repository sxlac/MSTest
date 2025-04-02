using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.DEE.Svc.Core.Constants;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.EventHandlers.Nsb;
using Signify.DEE.Svc.Core.Events;
using Signify.DEE.Svc.Core.Exceptions;
using Signify.DEE.Svc.Core.Infrastructure;
using Signify.DEE.Svc.Core.Messages.Commands;
using Signify.DEE.Svc.Core.Messages.Queries;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.EventHandlers.NSB;

public class RcmBillRequestAcceptedHandlerTests
{
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly FakeTransactionSupplier _transactionSupplier = A.Fake<FakeTransactionSupplier>();
    private readonly IApplicationTime _applicationTime = A.Fake<IApplicationTime>();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();
    private readonly TestableMessageHandlerContext _messageHandlerContext;
    private readonly RcmBillRequestAcceptedHandler _rcmBillRequestAcceptedHandler;

    public RcmBillRequestAcceptedHandlerTests()
    {
        _rcmBillRequestAcceptedHandler = new RcmBillRequestAcceptedHandler(
            A.Dummy<ILogger<RcmBillRequestAcceptedHandler>>(),
            _applicationTime,
            _transactionSupplier,
            _mediator,
            _publishObservability);
        _messageHandlerContext = new TestableMessageHandlerContext();
    }

    [Fact]
    public async Task Handle_When_BillIdIsFoundAndEvaluationIdPresent()
    {
        var billId = Guid.NewGuid();

        // Arrange
        var @event = new BillRequestAccepted
        {
            RCMBillId = billId,
            AdditionalDetails = new Dictionary<string, string>
            {
                { "EvaluationId", "123456789" }
            }
        };
        var billResult = A.Fake<DEEBilling>();
        A.CallTo(() => _mediator.Send(A<GetRcmBillingByBillId>._, A<CancellationToken>._)).Returns(billResult);

        // Act
        await _rcmBillRequestAcceptedHandler.Handle(@event, _messageHandlerContext);

        // Assert
        A.CallTo(() => _mediator.Send(A<CreateRcmBilling>._, CancellationToken.None)).MustHaveHappenedOnceExactly();

        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>.That.Matches(c =>
                c.EventType == Observability.RcmBilling.BillAcceptedSuccessEvent), true))
            .MustHaveHappenedOnceExactly();

        _transactionSupplier.AssertCommit();
    }

    [Fact]
    public async Task Handle_When_BillIdIsFoundAndEvaluationIdNotPresent()
    {
        var billId = Guid.NewGuid();

        // Arrange
        var @event = new BillRequestAccepted
        {
            RCMBillId = billId,
            AdditionalDetails = new Dictionary<string, string>()
        };
        var billResult = A.Fake<DEEBilling>();
        A.CallTo(() => _mediator.Send(A<GetRcmBillingByBillId>._, A<CancellationToken>._)).Returns(billResult);

        // Act
        await _rcmBillRequestAcceptedHandler.Handle(@event, _messageHandlerContext);

        // Assert
        A.CallTo(() => _mediator.Send(A<CreateRcmBilling>._, CancellationToken.None)).MustHaveHappenedOnceExactly();

        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>.That.Matches(c =>
                c.EventType == Observability.RcmBilling.BillAcceptedSuccessEvent), true))
            .MustHaveHappenedOnceExactly();

        _transactionSupplier.AssertCommit();
    }

    [Fact]
    public async Task Handle_When_BillIdIsFoundAndAdditionalDetailsNotPresent()
    {
        var billId = Guid.NewGuid();

        // Arrange
        var @event = new BillRequestAccepted
        {
            RCMBillId = billId
        };
        var billResult = A.Fake<DEEBilling>();
        A.CallTo(() => _mediator.Send(A<GetRcmBillingByBillId>._, A<CancellationToken>._)).Returns(billResult);

        // Act
        await _rcmBillRequestAcceptedHandler.Handle(@event, _messageHandlerContext);

        // Assert
        A.CallTo(() => _mediator.Send(A<GetRcmBillingByBillId>._, CancellationToken.None)).MustHaveHappenedOnceExactly();

        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>.That.Matches(c =>
                c.EventType == Observability.RcmBilling.BillAcceptedSuccessEvent), true))
            .MustHaveHappenedOnceExactly();

        _transactionSupplier.AssertCommit();
    }

    [Fact]
    public async Task Handle_When_BillIdIsNotFoundAndEvaluationIdPresent()
    {
        var billId = Guid.NewGuid();

        // Arrange
        var @event = new BillRequestAccepted
        {
            RCMBillId = billId,
            AdditionalDetails = new Dictionary<string, string>()
            {
                { "EvaluationId", "123456789" }
            }
        };
        A.CallTo(() => _mediator.Send(A<GetRcmBillingByBillId>._, A<CancellationToken>._)).Returns((DEEBilling)null);

        // Act
        await Assert.ThrowsAsync<BillNotFoundException>(async () => await _rcmBillRequestAcceptedHandler.Handle(@event, _messageHandlerContext));

        // Assert
        A.CallTo(() => _mediator.Send(A<CreateRcmBilling>._, CancellationToken.None)).MustNotHaveHappened();

        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>.That.Matches(c =>
                c.EventType == Observability.RcmBilling.BillAcceptedNotFoundEvent), true))
            .MustHaveHappenedOnceExactly();

        _transactionSupplier.AssertRollback();
    }

    [Fact]
    public async Task Handle_When_BillIdIsNotFoundAndEvaluationIdNotPresent()
    {
        var billId = Guid.NewGuid();

        // Arrange
        var @event = new BillRequestAccepted
        {
            RCMBillId = billId
        };

        A.CallTo(() => _mediator.Send(A<GetRcmBillingByBillId>._, A<CancellationToken>._)).Returns((DEEBilling)null);

        // Act
        await _rcmBillRequestAcceptedHandler.Handle(@event, _messageHandlerContext);

        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>.That.Matches(c =>
                c.EventType == Observability.RcmBilling.BillAcceptedNotTrackedEvent), true))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _mediator.Send(A<CreateRcmBilling>._, CancellationToken.None)).MustNotHaveHappened();

        _transactionSupplier.AssertCommit();
    }

    [Fact]
    public async Task Handle_When_BillIdIsFoundAndAdditionalDetailsIsNull()
    {
        var billId = Guid.NewGuid();
        // Arrange
        var @event = new BillRequestAccepted
        {
            RCMBillId = billId,
            AdditionalDetails = null
        };
        A.CallTo(() => _mediator.Send(A<GetRcmBillingByBillId>._, A<CancellationToken>._)).Returns(new DEEBilling
        {
            Id = 1,
            BillId = billId.ToString(),
            CreatedDateTime = DateTime.UtcNow
        });

        // Act
        await _rcmBillRequestAcceptedHandler.Handle(@event, _messageHandlerContext);

        A.CallTo(() => _mediator.Send(A<CreateRcmBilling>._, CancellationToken.None)).MustHaveHappenedOnceExactly();

        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>.That.Matches(c =>
                c.EventType == Observability.RcmBilling.BillAcceptedSuccessEvent), true))
            .MustHaveHappenedOnceExactly();

        _transactionSupplier.AssertCommit();
    }

    [Fact]
    public async Task Handle_When_BillIdIsNotFoundAndAdditionalDetailsIsNull()
    {
        var billId = Guid.NewGuid();
        // Arrange
        var message = new BillRequestAccepted
        {
            RCMBillId = billId,
            AdditionalDetails = null
        };
        A.CallTo(() => _mediator.Send(A<GetRcmBillingByBillId>._, A<CancellationToken>._)).Returns<DEEBilling>(null);
        // Act
        await _rcmBillRequestAcceptedHandler.Handle(message, _messageHandlerContext);
        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>.That.Matches(c =>
                    c.EventType == Observability.RcmBilling.BillAcceptedNotTrackedEvent &&
                    c.EvaluationId == 0),
                true))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreateRcmBilling>._, CancellationToken.None)).MustNotHaveHappened();
        _transactionSupplier.AssertCommit();
    }
}