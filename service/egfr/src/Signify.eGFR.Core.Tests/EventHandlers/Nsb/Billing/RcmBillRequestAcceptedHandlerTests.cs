using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NsbEventHandlers;
using NServiceBus.Testing;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using Signify.eGFR.Core.Commands;
using Signify.eGFR.Core.Constants;
using Signify.eGFR.Core.Data.Entities;
using Signify.eGFR.Core.Events;
using Signify.eGFR.Core.Exceptions;
using Signify.eGFR.Core.Queries;
using Xunit;

namespace Signify.eGFR.Core.Tests.EventHandlers.Nsb.Billing;

public class RcmBillRequestAcceptedHandlerTests
{
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly FakeTransactionSupplier _transactionSupplier = A.Fake<FakeTransactionSupplier>();
    private readonly FakeApplicationTime _applicationTime = new();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();
    private readonly TestableMessageHandlerContext _messageHandlerContext;
    private readonly RcmBillRequestAcceptedHandler _rcmBillRequestAcceptedHandler;

    public RcmBillRequestAcceptedHandlerTests()
    {
        _rcmBillRequestAcceptedHandler = new RcmBillRequestAcceptedHandler(
            A.Dummy<ILogger<RcmBillRequestAcceptedHandler>>(),
            _mediator,
            _transactionSupplier,
            _publishObservability,
            _applicationTime);
        _messageHandlerContext = new TestableMessageHandlerContext();
    }

    [Fact]
    public async Task Handle_When_BillIdIsFoundAndEvaluationIdPresent()
    {
        var billId = Guid.NewGuid();
        const long evaluationId = 123456;

        // Arrange
        var @event = new BillRequestAccepted
        {
            RCMBillId = billId,
            AdditionalDetails = new Dictionary<string, string>
            {
                { "EvaluationId", evaluationId.ToString() }
            }
        };
        var billResult = A.Fake<QueryBillRequestSentResult>(
            options =>
                options.WithArgumentsForConstructor(
                    [A.Fake<BillRequestSent>()])
        );
        A.CallTo(() => _mediator.Send(A<QueryBillRequestSent>._, A<CancellationToken>._))
            .Returns(billResult);

        // Act
        await _rcmBillRequestAcceptedHandler.Handle(@event, _messageHandlerContext);

        // Assert
        A.CallTo(() => _mediator.Send(A<AddOrUpdateBillRequestSent>._, CancellationToken.None)).MustHaveHappenedOnceExactly();

        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>.That.Matches(c =>
                c.EventType == Observability.RcmBilling.BillAcceptedSuccessEvent && c.EvaluationId == evaluationId), true))
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
        var billResult = A.Fake<QueryBillRequestSentResult>();
        A.CallTo(() => _mediator.Send(A<QueryBillRequestSent>._, A<CancellationToken>._)).Returns(billResult);

        // Act
        await _rcmBillRequestAcceptedHandler.Handle(@event, _messageHandlerContext);

        // Assert
        A.CallTo(() => _mediator.Send(A<AddOrUpdateBillRequestSent>._, CancellationToken.None)).MustHaveHappenedOnceExactly();

        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>.That.Matches(c =>
                c.EventType == Observability.RcmBilling.BillAcceptedSuccessEvent && c.EvaluationId == 0), true))
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
        var billResult = A.Fake<QueryBillRequestSentResult>();
        A.CallTo(() => _mediator.Send(A<QueryBillRequestSent>._, A<CancellationToken>._)).Returns(billResult);
        
        // Act
        await _rcmBillRequestAcceptedHandler.Handle(@event, _messageHandlerContext);
        
        // Assert
        A.CallTo(() => _mediator.Send(A<QueryBillRequestSent>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
    
        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>.That.Matches(c =>
                c.EventType == Observability.RcmBilling.BillAcceptedSuccessEvent && c.EvaluationId == 0), true))
            .MustHaveHappenedOnceExactly();
        
        _transactionSupplier.AssertCommit();
    }
    
    [Fact]
    public async Task Handle_When_BillIdIsNotFoundAndAdditionalDetailsIsNotPresent()
    {
        var billId = Guid.NewGuid();

        // Arrange
        var @event = new BillRequestAccepted
        {
            RCMBillId = billId,
        };
        var billResult = A.Fake<QueryBillRequestSentResult>();
        A.CallTo(() => _mediator.Send(A<QueryBillRequestSent>._, A<CancellationToken>._)).Returns(billResult);

        // Act
        await _rcmBillRequestAcceptedHandler.Handle(@event, _messageHandlerContext);

        // Assert
        A.CallTo(() => _mediator.Send(A<AddOrUpdateBillRequestSent>._, CancellationToken.None)).MustHaveHappenedOnceExactly();

        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>.That.Matches(c =>
                c.EventType == Observability.RcmBilling.BillAcceptedSuccessEvent && c.EvaluationId == 0), true))
            .MustHaveHappenedOnceExactly();

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
        var billResult = A.Fake<QueryBillRequestSentResult>();
        A.CallTo(() => _mediator.Send(A<QueryBillRequestSent>._, A<CancellationToken>._)).Returns(billResult);
        
        // Act
        await _rcmBillRequestAcceptedHandler.Handle(@event, _messageHandlerContext);
        
        // Assert
        A.CallTo(() => _mediator.Send(A<QueryBillRequestSent>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
    
        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>.That.Matches(c =>
                c.EventType == Observability.RcmBilling.BillAcceptedSuccessEvent && c.EvaluationId == 0), true))
            .MustHaveHappenedOnceExactly();
        
        _transactionSupplier.AssertCommit();
    }
    
    [Fact]
    public async Task Handle_When_BillIdIsNotFoundAndEvaluationIdPresent()
    {
        var billId = Guid.NewGuid();
        const long evaluationId = 12345678;
        
        // Arrange
        var @event = new BillRequestAccepted
        {
            RCMBillId = billId,
            AdditionalDetails = new Dictionary<string, string>()
            {
                { "EvaluationId", evaluationId.ToString()}
            }
        };
        A.CallTo(() => _mediator.Send(A<QueryBillRequestSent>._, A<CancellationToken>._)).Returns((QueryBillRequestSentResult)null);
        
        // Act
        await Assert.ThrowsAsync<BillNotFoundException>(async () => await _rcmBillRequestAcceptedHandler.Handle(@event, _messageHandlerContext));
        
        // Assert
        A.CallTo(() => _mediator.Send(A<AddOrUpdateBillRequestSent>._, CancellationToken.None)).MustNotHaveHappened();
        
        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>.That.Matches(c =>
                c.EventType == Observability.RcmBilling.BillAcceptedNotFoundEvent && c.EvaluationId == evaluationId), true))
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
        
        A.CallTo(() => _mediator.Send(A<QueryBillRequestSent>._, A<CancellationToken>._)).Returns((QueryBillRequestSentResult)null);
        
        // Act
        await _rcmBillRequestAcceptedHandler.Handle(@event, _messageHandlerContext);
        
        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>.That.Matches(c =>
                c.EventType == Observability.RcmBilling.BillAcceptedNotTrackedEvent && c.EvaluationId == 0), true))
            .MustHaveHappenedOnceExactly();
        
        A.CallTo(() => _mediator.Send(A<AddOrUpdateBillRequestSent>._, CancellationToken.None)).MustNotHaveHappened();
        
        _transactionSupplier.AssertCommit();
    }
    
    [Fact]
    public async Task Handle_When_BillIdIsNotFoundAndAdditionalDetailsIsNull()
    {
        var billId = Guid.NewGuid();
        
        // Arrange
        var @event = new BillRequestAccepted
        {
            RCMBillId = billId,
            AdditionalDetails = null
        };
        
        A.CallTo(() => _mediator.Send(A<QueryBillRequestSent>._, A<CancellationToken>._)).Returns((QueryBillRequestSentResult)null);
        
        // Act
        await _rcmBillRequestAcceptedHandler.Handle(@event, _messageHandlerContext);
        
        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>.That.Matches(c =>
                c.EventType == Observability.RcmBilling.BillAcceptedNotTrackedEvent && c.EvaluationId == 0), true))
            .MustHaveHappenedOnceExactly();
        
        A.CallTo(() => _mediator.Send(A<AddOrUpdateBillRequestSent>._, CancellationToken.None)).MustNotHaveHappened();
        
        _transactionSupplier.AssertCommit();
    }
}