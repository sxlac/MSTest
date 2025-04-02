using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using UacrNsbEvents;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.Dps.Observability.Library.Services;
using Signify.uACR.Core.Commands;
using Signify.uACR.Core.Data;
using Signify.uACR.Core.Data.Entities;
using Signify.uACR.Core.EventHandlers.Nsb;
using Signify.uACR.Core.Queries;
using Xunit;

namespace Signify.uACR.Core.Tests.EventHandlers.Nsb;

public class OrderCreationEventHandlerTests
{
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly ITransactionSupplier _transactionSupplier = A.Fake<ITransactionSupplier>();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();
    private readonly FakeApplicationTime _applicationTime = new();
    
    private OrderCreationEventHandler CreateSubject()
        => new(A.Dummy<ILogger<OrderCreationEventHandler>>(), _mediator,
            _mapper, _transactionSupplier, _publishObservability, _applicationTime);

    [Theory]
    [MemberData(nameof(VariousOrderCreationEvent))]
    public async Task Handle_WithMessage_SendsEvaluationProcessedEvent(OrderCreationEvent request, Events.Akka.OrderCreationEvent orderCreationEvent)
    {
        // Arrange
        A.CallTo(() => _mapper.Map<Events.Akka.OrderCreationEvent>(A<OrderCreationEvent>._))
            .Returns(orderCreationEvent);
        A.CallTo(() => _mediator.Send(A<QueryExamByEvaluation>._, A<CancellationToken>._))
            .Returns(new Exam());
        var context = new TestableMessageHandlerContext();

        // Act
        var orderCreationHandler = CreateSubject();
        await orderCreationHandler.Handle(request, context);

        // Assert
        A.CallTo(() =>
                _mediator.Send(
                    A<PublishOrderCreation>.That.Matches(e => e.Event.EvaluationId == request.EvaluationId &&
                                                              e.Event.EventId == request.EventId), A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() =>
            _mediator.Send(
                A<UpdateExamStatus>.That.Matches(e =>
                    e.ExamStatus.StatusCode == ExamStatusCode.OrderRequested &&
                    e.ExamStatus.EvaluationId == request.EvaluationId &&
                    e.ExamStatus.EventId == request.EventId), A<CancellationToken>._)).MustHaveHappened();
    }
    
    
    public static IEnumerable<object[]> VariousOrderCreationEvent()
    {
        
        long evaluationId = 123;
        Guid eventId = Guid.NewGuid();
        string barcode = "lgc-0000-0000-0000-0000";
        var request = new OrderCreationEvent()
        {
            EvaluationId = evaluationId,
            EventId = eventId,
            Context = new Dictionary<string, string>{ {"barcode", barcode }}
        };

        var orderCreationEvent = new Events.Akka.OrderCreationEvent
        {
            EvaluationId = evaluationId,
            EventId = eventId,
            Context = new Dictionary<string, string>{ {"barcode", barcode}}
        };
        
        yield return [request, orderCreationEvent];
        
        evaluationId = 123;
        eventId = Guid.NewGuid();
        barcode = "abc";
        request = new OrderCreationEvent()
        {
            EvaluationId = evaluationId,
            EventId = eventId,
            Context = new Dictionary<string, string>{ {"barcode", barcode }}
        };

        orderCreationEvent = new Events.Akka.OrderCreationEvent
        {
            EvaluationId = evaluationId,
            EventId = eventId,
            Context = new Dictionary<string, string>{ {"barcode", barcode}}
        };
        yield return [request, orderCreationEvent];
        
        evaluationId = 123;
        eventId = Guid.NewGuid();
        barcode = "";
        request = new OrderCreationEvent()
        {
            EvaluationId = evaluationId,
            EventId = eventId,
            Context = new Dictionary<string, string>{ {"barcode", barcode }}
        };

        orderCreationEvent = new Events.Akka.OrderCreationEvent
        {
            EvaluationId = evaluationId,
            EventId = eventId,
            Context = new Dictionary<string, string>{ {"barcode", barcode}}
        };
        yield return [request, orderCreationEvent];
        
        evaluationId = 123;
        eventId = Guid.NewGuid();
        request = new OrderCreationEvent()
        {
            EvaluationId = evaluationId,
            EventId = eventId,
            Context = new Dictionary<string, string>{ {"barcode", null }}
        };

        orderCreationEvent = new Events.Akka.OrderCreationEvent
        {
            EvaluationId = evaluationId,
            EventId = eventId,
            Context = new Dictionary<string, string>{ {"barcode", null}}
        };
        yield return [request, orderCreationEvent];
        
        evaluationId = 123;
        eventId = Guid.NewGuid();
        request = new OrderCreationEvent()
        {
            EvaluationId = evaluationId,
            EventId = eventId
        };

        orderCreationEvent = new Events.Akka.OrderCreationEvent
        {
            EvaluationId = evaluationId,
            EventId = eventId
        };
        yield return [request, orderCreationEvent];
    }
}