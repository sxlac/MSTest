using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.HBA1CPOC.Messages.Events;
using Signify.HBA1CPOC.Svc.Core.EventHandlers;
using Signify.HBA1CPOC.Svc.Core.Events;
using Signify.HBA1CPOC.Svc.Core.Infrastructure.Observability;
using Signify.HBA1CPOC.Svc.Core.Models;
using Signify.HBA1CPOC.Svc.Core.Queries;
using Signify.HBA1CPOC.Svc.Core.Tests.Mocks.StaticEntity;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.HBA1CPOC.Svc.Core.Tests.EventHandlers.Nsb;

public class EvaluationFinalizedHandlerTest
{
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly EvaluationFinalizedHandler _evaluationFinalizedHandler;
    private readonly TestableMessageSession _session = new();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();
        
    public EvaluationFinalizedHandlerTest()
    {
        _evaluationFinalizedHandler = new EvaluationFinalizedHandler(A.Dummy<ILogger<EvaluationFinalizedHandler>>(), _session, _mapper, _publishObservability);
    }

    [Fact]
    public async Task EvaluationFinalizedHandler_WhenProductCodeIsNotHBA1CPOC()
    {
        var @event = new EvaluationFinalizedEvent
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
            Products = [new Product("HHRA"), new Product("CKD")]
        };
        await _evaluationFinalizedHandler.Handle(@event, CancellationToken.None);
        _session.SentMessages.Length.Should().Be(0);
    }       

    [Fact]
    public async Task EvaluationFinalizedHandler_ProductCodeCaseCheck()
    {
        var evaluationAnswers = new EvaluationAnswers
        {
            A1CPercent = "6", ExpirationDate = DateTime.UtcNow, IsHBA1CEvaluation = true
        };
        A.CallTo(() => _mediator.Send(A<CheckHBA1CPOCEval>._, CancellationToken.None)).Returns(evaluationAnswers);
        A.CallTo(() => _mapper.Map<EvalReceived>(A<EvaluationFinalizedEvent>._)).Returns(new EvalReceived());
        await _evaluationFinalizedHandler.Handle(StaticMockEntities.EvaluationFinalizedEvent, CancellationToken.None);
        _session.SentMessages.Length.Should().Be(1, "Each request send one message internally per request");
    }
}