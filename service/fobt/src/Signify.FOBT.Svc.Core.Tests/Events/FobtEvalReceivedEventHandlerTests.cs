using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using FobtNsbEvents;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.FOBT.Svc.Core.Infrastructure.Observability;
using Signify.FOBT.Svc.Core.Queries;
using System.Threading.Tasks;
using System;
using Xunit;
using Fobt = Signify.FOBT.Svc.Core.Data.Entities.FOBT;

namespace Signify.FOBT.Svc.Core.Tests.Events;

public class FobtEvalReceivedEventHandlerTests
{
    private readonly ILogger<FobtEvalReceivedEventHandler> _logger = A.Dummy<ILogger<FobtEvalReceivedEventHandler>>();
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();

    [Fact]
    public async Task EndorseEvaluationFinalizedEventHandler_EvaluationPreviouslyFinalizedWithSameDateOfService_ReturnWithNoMessagesPublished()
    {
        // Arrange
        var endorseEvaluationFinalizedEvent = Mocks.Models.FobtEvalReceivedMock.BuildFobtEvalReceived();
        var fobtRecord = Mocks.Models.FobtEntityMock.BuildFobt();
        fobtRecord.DateOfService = endorseEvaluationFinalizedEvent.DateOfService;
        A.CallTo(() => _mediator.Send(A<GetFOBT>._, default)).Returns(fobtRecord);
        var handler = new FobtEvalReceivedEventHandler(_logger, _mapper, _mediator, _publishObservability);
        var context = new TestableInvokeHandlerContext();

        // Act
        await handler.Handle(endorseEvaluationFinalizedEvent, context);

        // Assert
        context.PublishedMessages.Length.Should().Be(0);
        A.CallTo(() => _mediator.Send(A<GetFOBT>._, default)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<CheckFOBTEval>._, default)).MustNotHaveHappened();
    }

    [Fact]
    public async Task EndorseEvaluationFinalizedEventHandler_EvaluationPreviouslyFinalizedWithNullDateOfService_ReturnWithNoMessagesPublished()
    {
        // Arrange
        var endorseEvaluationFinalizedEvent = Mocks.Models.FobtEvalReceivedMock.BuildFobtEvalReceived();
        endorseEvaluationFinalizedEvent.DateOfService = null;
        var fobtRecord = Mocks.Models.FobtEntityMock.BuildFobt();
        fobtRecord.DateOfService = DateTime.UtcNow;
        A.CallTo(() => _mediator.Send(A<GetFOBT>._, default)).Returns(fobtRecord);
        var handler = new FobtEvalReceivedEventHandler(_logger, _mapper, _mediator, _publishObservability);
        var context = new TestableInvokeHandlerContext();

        // Act
        await handler.Handle(endorseEvaluationFinalizedEvent, context);

        // Assert
        context.PublishedMessages.Length.Should().Be(0);
        A.CallTo(() => _mediator.Send(A<GetFOBT>._, default)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<CheckFOBTEval>._, default)).MustNotHaveHappened();
    }

    [Fact]
    public async Task EndorseEvaluationFinalizedEventHandler_EvaluationPreviouslyFinalizedWithDifferentDateOfService_PublishDateOfServiceUpdate()
    {
        // Arrange
        var endorseEvaluationFinalizedEvent = Mocks.Models.FobtEvalReceivedMock.BuildFobtEvalReceived();
        var fobtRecord = Mocks.Models.FobtEntityMock.BuildFobt();
        fobtRecord.DateOfService = endorseEvaluationFinalizedEvent.DateOfService?.AddSeconds(-1);
        A.CallTo(() => _mediator.Send(A<GetFOBT>._, default)).Returns(fobtRecord);
        var handler = new FobtEvalReceivedEventHandler(_logger, _mapper, _mediator, _publishObservability);
        var context = new TestableInvokeHandlerContext();

        // Act
        await handler.Handle(endorseEvaluationFinalizedEvent, context);

        // Assert
        context.PublishedMessages.Length.Should().Be(1);
        A.CallTo(() => _mediator.Send(A<GetFOBT>._, default)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<CheckFOBTEval>._, default)).MustNotHaveHappened();
    }

    [Fact]
    public async Task EndorseEvaluationFinalizedEventHandler_EvaluationPerformed_PublishEvaluationReceived()
    {
        // Arrange
        var endorseEvaluationFinalizedEvent = Mocks.Models.FobtEvalReceivedMock.BuildFobtEvalReceived();
        A.CallTo(() => _mediator.Send(A<GetFOBT>._, default)).Returns((Fobt)null);
        A.CallTo(() => _mediator.Send(A<CheckFOBTEval>._, default)).Returns("12345678996542");
        var handler = new FobtEvalReceivedEventHandler(_logger, _mapper, _mediator, _publishObservability);
        var context = new TestableInvokeHandlerContext();

        // Act
        await handler.Handle(endorseEvaluationFinalizedEvent, context);

        // Assert
        context.PublishedMessages.Length.Should().Be(1);
        A.CallTo(() => _mediator.Send(A<GetFOBT>._, default)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<CheckFOBTEval>._, default)).MustHaveHappened();
    }

    [Fact]
    public async Task EndorseEvaluationFinalizedEventHandler_EvaluationNotPerformed_PublishEvaluationReceived()
    {
        // Arrange
        var endorseEvaluationFinalizedEvent = Mocks.Models.FobtEvalReceivedMock.BuildFobtEvalReceived();
        A.CallTo(() => _mediator.Send(A<GetFOBT>._, default)).Returns((Fobt)null);
        A.CallTo(() => _mediator.Send(A<CheckFOBTEval>._, default)).Returns(string.Empty);
        var handler = new FobtEvalReceivedEventHandler(_logger, _mapper, _mediator, _publishObservability);
        var context = new TestableInvokeHandlerContext();

        // Act
        await handler.Handle(endorseEvaluationFinalizedEvent, context);

        // Assert
        context.PublishedMessages.Length.Should().Be(1);
        A.CallTo(() => _mediator.Send(A<GetFOBT>._, default)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<CheckFOBTEval>._, default)).MustHaveHappened();
    }
}