using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.HBA1CPOC.Messages.Events;
using Signify.HBA1CPOC.Svc.Core.Commands;
using Signify.HBA1CPOC.Svc.Core.Events;
using Signify.HBA1CPOC.Svc.Core.Infrastructure.Observability;
using Signify.HBA1CPOC.Svc.Core.Models;
using Signify.HBA1CPOC.Svc.Core.Queries;
using Signify.HBA1CPOC.Svc.Core.Tests.Mocks.StaticEntity;
using Signify.HBA1CPOC.Svc.Core.Tests.Utilities;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.HBA1CPOC.Svc.Core.Tests.Commands;

public class CreateHbA1cPocHandlerTest : IClassFixture<MockDbFixture>
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly CreateHbA1CPocHandler _createHbA1cPocHandler;
    private readonly TestableMessageHandlerContext _messageHandlerContext;
    private readonly IPublishObservability _publishObservability;
    public CreateHbA1cPocHandlerTest(MockDbFixture mockDbFixture)
    {
        _messageHandlerContext = new TestableMessageHandlerContext();
        var logger = A.Fake<ILogger<CreateHbA1CPocHandler>>();
        _mediator = A.Fake<IMediator>();
        _mapper = A.Fake<IMapper>();
        _publishObservability = A.Fake<IPublishObservability>();
        _createHbA1cPocHandler =
            new CreateHbA1CPocHandler(logger, _mapper, mockDbFixture.Context, _mediator, _publishObservability);
    }

    [Fact]
    public async Task CreateHbA1cPocHandler_EvaluationExists_Different_DOS()
    {
        var @event = new CreateHbA1CPoc
        {
            ApplicationId = "Signify.Evaluation.Service",
            AppointmentId = 1000084716,
            ClientId = 14,
            CreatedDateTime = DateTimeOffset.UtcNow,
            DateOfService = DateTime.UtcNow,
            DocumentPath = null,
            EvaluationId = 324357,
            EvaluationTypeId = 1,
            FormVersionId = 0,
            Location = new Location(32.925496267, 32.925496267),
            MemberId = 11990396,
            MemberPlanId = 21074285,
            ProviderId = 42879,
            ReceivedDateTime = DateTime.UtcNow,
            UserName = "vastest1",
            Products = [new Product("HHRA"), new Product("HBA1CPOC")]
        };
        await _createHbA1cPocHandler.Handle(@event, _messageHandlerContext);
        _messageHandlerContext.SentMessages.Length.Should().Be(1);
    }

    [Fact]
    public async Task UpdateDateOfService_Should_Call_UpdateHBA1CPOC()
    {
        DateTime? eventDos = DateTime.UtcNow;
        var hba1cpoc = new Core.Data.Entities.HBA1CPOC();
        const int evaluationId = 1;
        await _createHbA1cPocHandler.UpdateDateOfService(eventDos, hba1cpoc, evaluationId, _messageHandlerContext);

        _messageHandlerContext.SentMessages.Length.Should().Be(1);
    }

    [Fact]
    public async Task UpdateDateOfService_Should_Not_Call_UpdateHBA1CPOC()
    {
        var hba1cpoc = new Core.Data.Entities.HBA1CPOC();
        const int evaluationId = 1;
        await _createHbA1cPocHandler.UpdateDateOfService(default, hba1cpoc, evaluationId, _messageHandlerContext);

        _messageHandlerContext.SentMessages.Length.Should().Be(0);
    }

    [Fact]
    public async Task UpdateDateOfService_Should_Not_Call_UpdateHBA1CPOC_When_null()
    {
        var hba1cpoc = new Core.Data.Entities.HBA1CPOC
        {
            DateOfService = DateTime.UtcNow
        };
        const int evaluationId = 1;
        await _createHbA1cPocHandler.UpdateDateOfService(null, hba1cpoc, evaluationId, _messageHandlerContext);

        _messageHandlerContext.SentMessages.Length.Should().Be(0);
    }

    [Fact]
    public async Task CreateHbA1cPocHandler_WhenEvaluationApiReturnEmpty()
    {
        A.CallTo(() => _mediator.Send(A<CheckHBA1CPOCEval>._, CancellationToken.None)).Returns(new EvaluationAnswers());
        await _createHbA1cPocHandler.Handle(StaticMockEntities.CreateHbA1cPoc, _messageHandlerContext);
        _messageHandlerContext.SentMessages.Length.Should().Be(0);
    }

    [Fact]
    public async Task CreateHbA1cPocHandler_WhenEvaluationApiReturnFalse()
    {
        var evaluationAnswers = new EvaluationAnswers
            { A1CPercent = "6", ExpirationDate = DateTime.UtcNow, IsHBA1CEvaluation = false };
        A.CallTo(() => _mediator.Send(A<CheckHBA1CPOCEval>._, CancellationToken.None)).Returns(evaluationAnswers);
        await _createHbA1cPocHandler.Handle(StaticMockEntities.CreateHbA1cPoc, _messageHandlerContext);
        _messageHandlerContext.SentMessages.Length.Should().Be(0);
    }
    [Fact]
    public async Task CreateHbA1cPocHandler_EndpointPublishCheck()
    {
        var evaluationAnswers = new EvaluationAnswers
            { A1CPercent = "6", ExpirationDate = DateTime.UtcNow, IsHBA1CEvaluation = true };
        A.CallTo(() => _mediator.Send(A<CheckHBA1CPOCEval>._, CancellationToken.None)).Returns(evaluationAnswers);
        A.CallTo(() => _mapper.Map<EvalReceived>(A<EvaluationFinalizedEvent>._)).Returns(new EvalReceived());
        await _createHbA1cPocHandler.Handle(StaticMockEntities.CreateHbA1cPoc, _messageHandlerContext);
        _messageHandlerContext.PublishedMessages.Length.Should().Be(1);
    }

    [Fact]
    public async Task CreateHbA1cPocHandlerEndpointPublishType()
    {
        var evaluationAnswers = new EvaluationAnswers
            { A1CPercent = "6", ExpirationDate = DateTime.UtcNow, IsHBA1CEvaluation = true };
        A.CallTo(() => _mediator.Send(A<CheckHBA1CPOCEval>._, CancellationToken.None)).Returns(evaluationAnswers);
        A.CallTo(() => _mapper.Map<EvalReceived>(A<EvaluationFinalizedEvent>._)).Returns(new EvalReceived());
        await _createHbA1cPocHandler.Handle(StaticMockEntities.CreateHbA1cPoc, _messageHandlerContext);
        _messageHandlerContext.PublishedMessages.Length.Should().Be(1);
        _messageHandlerContext.PublishedMessages[0].Message.Should().BeOfType<EvalReceived>();
    }

    [Fact]
    public async Task CreateHbA1cPocHandlerNumberOfTimesCalled()
    {
        var evaluationAnswers = new EvaluationAnswers
            { A1CPercent = "6", ExpirationDate = DateTime.UtcNow, IsHBA1CEvaluation = true };
        A.CallTo(() => _mediator.Send(A<CheckHBA1CPOCEval>._, CancellationToken.None)).Returns(evaluationAnswers);
        A.CallTo(() => _mapper.Map<EvalReceived>(A<CreateHbA1CPoc>._)).Returns(new EvalReceived());
        await _createHbA1cPocHandler.Handle(StaticMockEntities.CreateHbA1cPoc, _messageHandlerContext);
        A.CallTo(() => _mediator.Send(A<CheckHBA1CPOCEval>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mapper.Map<EvalReceived>(A<CreateHbA1CPoc>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task CreateHbA1cPocHandlerNumberOfEventsPublishedPerReq()
    {
        var evaluationAnswers = new EvaluationAnswers
            { A1CPercent = "6", ExpirationDate = DateTime.UtcNow, IsHBA1CEvaluation = true };
        A.CallTo(() => _mediator.Send(A<CheckHBA1CPOCEval>._, CancellationToken.None)).Returns(evaluationAnswers);
        A.CallTo(() => _mapper.Map<EvalReceived>(A<CreateHbA1CPoc>._)).Returns(new EvalReceived());
        await _createHbA1cPocHandler.Handle(StaticMockEntities.CreateHbA1cPoc, _messageHandlerContext);
        _messageHandlerContext.PublishedMessages.Length.Should().Be(1, "Each request will publish one message");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_WhetherLabPerformedOrNot_PublishesEvalReceived(bool isLabPerformed)
    {
        var @event = new CreateHbA1CPoc
        {
            EvaluationId = StaticMockEntities.EvaluationFinalizedEvent.EvaluationId,
            Products = StaticMockEntities.EvaluationFinalizedEvent.Products
        };

        A.CallTo(() => _mediator.Send(A<CheckHBA1CPOCEval>.That.Matches(
                c => c.EvaluationId == @event.EvaluationId), A<CancellationToken>._)
        ).Returns(Task.FromResult(new EvaluationAnswers { IsHBA1CEvaluation = isLabPerformed }));

        await _createHbA1cPocHandler.Handle(@event, _messageHandlerContext);

        _messageHandlerContext.PublishedMessages.Should().ContainSingle();
        _messageHandlerContext.PublishedMessages.First().Message.Should().BeOfType<EvalReceived>()
            .Which.IsLabPerformed.Should().Be(isLabPerformed);
    }
}