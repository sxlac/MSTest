using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using EgfrNsbEvents;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NsbEventHandlers;
using NServiceBus.Testing;
using Signify.Dps.Observability.Library.Services;
using Signify.eGFR.Core.Commands;
using Signify.eGFR.Core.Data;
using Signify.eGFR.Core.Data.Entities;
using Signify.eGFR.Core.Queries;
using Xunit;

namespace Signify.eGFR.Core.Tests.EventHandlers.Nsb.OrderCreation;

public class OrderCreationEventHandlerTests
{
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly ITransactionSupplier _transactionSupplier = A.Fake<ITransactionSupplier>();
    private readonly FakeApplicationTime _applicationTime = new();

    private OrderCreationEventHandler CreateSubject()
        => new(A.Dummy<ILogger<OrderCreationEventHandler>>(), _mediator, _transactionSupplier, A.Fake<IPublishObservability>(),
            _applicationTime, _mapper);

    [Theory]
    [MemberData(nameof(VariousOrderCreationEvent))]
    public async Task Handle_WithMessage_SendsEvaluationProcessedEvent(OrderCreationEvent request, Events.Akka.OrderCreationEvent orderCreationEvent)
    {
        // Arrange
        A.CallTo(() => _mapper.Map<Events.Akka.OrderCreationEvent>(A<OrderCreationEvent>._))
            .Returns(orderCreationEvent);
        A.CallTo(() => _mediator.Send(A<QueryExam>._, A<CancellationToken>._))
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
        var eventId = Guid.NewGuid();
        var barcode = "lgc-0000-0000-0000-0000";
        var request = new OrderCreationEvent
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
        request = new OrderCreationEvent
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
        request = new OrderCreationEvent
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
        request = new OrderCreationEvent
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
        request = new OrderCreationEvent
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