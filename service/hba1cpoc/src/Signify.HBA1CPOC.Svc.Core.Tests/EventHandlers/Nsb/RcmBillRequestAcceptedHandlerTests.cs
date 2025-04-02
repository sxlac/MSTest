using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.HBA1CPOC.Svc.Core.Commands;
using Signify.HBA1CPOC.Svc.Core.Constants;
using Signify.HBA1CPOC.Svc.Core.Data.Entities;
using Signify.HBA1CPOC.Svc.Core.EventHandlers.Nsb;
using Signify.HBA1CPOC.Svc.Core.Events;
using Signify.HBA1CPOC.Svc.Core.Exceptions;
using Signify.HBA1CPOC.Svc.Core.Infrastructure.Observability;
using Signify.HBA1CPOC.Svc.Core.Infrastructure;
using Signify.HBA1CPOC.Svc.Core.Queries;
using Signify.HBA1CPOC.Svc.Core.Tests.Mocks.StaticEntity;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;
using Guid = System.Guid;

namespace Signify.HBA1CPOC.Svc.Core.Tests.EventHandlers.Nsb;

public class RcmBillRequestAcceptedHandlerTests
{
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly FakeTransactionSupplier _transactionSupplier = A.Fake<FakeTransactionSupplier>();
    private readonly IApplicationTime _applicationTime  = A.Fake<IApplicationTime>();
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
                { "EvaluationId", "123456789"}
            }
        };
        
        A.CallTo(() => _mediator.Send(A<GetRcmBillingByBillId>._, A<CancellationToken>._)).Returns(new HBA1CPOCRCMBilling 
        { 
            Id = 1,
            BillId = billId.ToString(),
            HBA1CPOCId = StaticMockEntities.Hba1Cpoc.HBA1CPOCId,
            CreatedDateTime = DateTime.UtcNow
        });
        
        // Act
        await _rcmBillRequestAcceptedHandler.Handle(@event, _messageHandlerContext);
        
        A.CallTo(() => _mediator.Send(A<CreateOrUpdateRCMBilling>._, CancellationToken.None)).MustHaveHappenedOnceExactly();

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
        
        A.CallTo(() => _mediator.Send(A<GetRcmBillingByBillId>._, A<CancellationToken>._)).Returns(new HBA1CPOCRCMBilling 
        { 
            Id = 1,
            BillId = billId.ToString(),
            HBA1CPOCId = StaticMockEntities.Hba1Cpoc.HBA1CPOCId,
            CreatedDateTime = DateTime.UtcNow
        });
        
        // Act
        await _rcmBillRequestAcceptedHandler.Handle(@event, _messageHandlerContext);
        
        A.CallTo(() => _mediator.Send(A<CreateOrUpdateRCMBilling>._, CancellationToken.None)).MustHaveHappenedOnceExactly();

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
        
        A.CallTo(() => _mediator.Send(A<GetRcmBillingByBillId>._, A<CancellationToken>._)).Returns(new HBA1CPOCRCMBilling 
        { 
            Id = 1,
            BillId = billId.ToString(),
            HBA1CPOCId = StaticMockEntities.Hba1Cpoc.HBA1CPOCId,
            CreatedDateTime = DateTime.UtcNow
        });
        
        // Act
        await _rcmBillRequestAcceptedHandler.Handle(@event, _messageHandlerContext);
        
        A.CallTo(() => _mediator.Send(A<CreateOrUpdateRCMBilling>._, CancellationToken.None)).MustHaveHappenedOnceExactly();

        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>.That.Matches(c =>
                c.EventType == Observability.RcmBilling.BillAcceptedSuccessEvent), true))
            .MustHaveHappenedOnceExactly();
        
        _transactionSupplier.AssertCommit();
    }

    [Fact]
    public async Task Handle_When_BillIdIsFoundAndAdditionalDetailsIsNull()
    {
        // Arrange
        var billId = Guid.NewGuid();
        var @event = new BillRequestAccepted
        {
            RCMBillId = billId,
            AdditionalDetails = null
        };
        A.CallTo(() => _mediator.Send(A<GetRcmBillingByBillId>._, A<CancellationToken>._)).Returns(new HBA1CPOCRCMBilling
        {
            Id = 1,
            BillId = billId.ToString(),
            HBA1CPOCId = StaticMockEntities.Hba1Cpoc.HBA1CPOCId,
            CreatedDateTime = DateTime.UtcNow
        });

        // Act
        await _rcmBillRequestAcceptedHandler.Handle(@event, _messageHandlerContext);

        A.CallTo(() => _mediator.Send(A<CreateOrUpdateRCMBilling>._, CancellationToken.None)).MustHaveHappenedOnceExactly();

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
            AdditionalDetails = new Dictionary<string, string>
            {
                { "EvaluationId", "123456789"}
            }
        };

        A.CallTo(() => _mediator.Send(A<GetRcmBillingByBillId>._, A<CancellationToken>._)).Returns((HBA1CPOCRCMBilling)null);
        
        await Assert.ThrowsAsync<BillIdNotFoundException>(async () => await _rcmBillRequestAcceptedHandler.Handle(@event, _messageHandlerContext));
        
        A.CallTo(() => _mediator.Send(A<CreateOrUpdateRCMBilling>._, CancellationToken.None)).MustNotHaveHappened();
        
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

        A.CallTo(() => _mediator.Send(A<GetRcmBillingByBillId>._, A<CancellationToken>._)).Returns((HBA1CPOCRCMBilling)null);
        
        // Act
        await _rcmBillRequestAcceptedHandler.Handle(@event, _messageHandlerContext);
        
        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>.That.Matches(c =>
                c.EventType == Observability.RcmBilling.BillAcceptedNotTrackedEvent), true))
            .MustHaveHappenedOnceExactly();
        
        A.CallTo(() => _mediator.Send(A<CreateOrUpdateRCMBilling>._, CancellationToken.None)).MustNotHaveHappened();
        
        _transactionSupplier.AssertCommit();
    }
    
    [Fact]
    public async Task Handle_When_BillIdIsFoundAndNullableFieldsArePresent()
    {
        var billId = Guid.NewGuid();
        
        // Arrange
        var @event = new BillRequestAccepted
        {
            RCMBillId = billId,
            ProviderId = null,
            MemberPlanId = null,
            SharedClientID = null,
            SubsidiaryId = null,
        };
        
        A.CallTo(() => _mediator.Send(A<GetRcmBillingByBillId>._, A<CancellationToken>._)).Returns(new HBA1CPOCRCMBilling 
        { 
            Id = 1,
            BillId = billId.ToString(),
            HBA1CPOCId = StaticMockEntities.Hba1Cpoc.HBA1CPOCId,
            CreatedDateTime = DateTime.UtcNow
        });
        
        // Act
        await _rcmBillRequestAcceptedHandler.Handle(@event, _messageHandlerContext);
        
        A.CallTo(() => _mediator.Send(A<CreateOrUpdateRCMBilling>._, CancellationToken.None)).MustHaveHappenedOnceExactly();

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
        A.CallTo(() => _mediator.Send(A<GetRcmBillingByBillId>._, A<CancellationToken>._)).Returns<HBA1CPOCRCMBilling>(null);
        // Act
        await _rcmBillRequestAcceptedHandler.Handle(message, _messageHandlerContext);
        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>.That.Matches(c =>
                    c.EventType == Observability.RcmBilling.BillAcceptedNotTrackedEvent &&
                    c.EvaluationId == 0),
                true))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreateOrUpdateRCMBilling>._, CancellationToken.None)).MustNotHaveHappened();
        _transactionSupplier.AssertCommit();
    }
}