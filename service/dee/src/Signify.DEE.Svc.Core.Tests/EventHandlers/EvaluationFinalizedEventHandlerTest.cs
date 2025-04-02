using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.DEE.Svc.Core.EventHandlers;
using Signify.DEE.Svc.Core.Events;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.EventHandlers;

public class EvaluationFinalizedEventHandlerTest
{
    private readonly EvaluationFinalizedEventHandler _evaluationFinalizedEventHandler;
    private readonly TestableMessageSession _endpointInstance;
    private static readonly FakeApplicationTime ApplicationTime = new();
    private readonly IPublishObservability _publishObservability;

    public EvaluationFinalizedEventHandlerTest()
    {
        _endpointInstance = new TestableMessageSession();
        _publishObservability = A.Fake<IPublishObservability>();
        _evaluationFinalizedEventHandler = new EvaluationFinalizedEventHandler(
            A.Dummy<ILogger<EvaluationFinalizedEventHandler>>(),
            _endpointInstance,
            _publishObservability);
    }

    [Fact]
    public async Task When_ProductCode_Is_Not_Equals_DEE_Then_Evaluation_Should_NotHaveHappened()
    {
        var message = new EvaluationFinalizedEvent { Products = new List<Product> { new("Other") } };
        await _evaluationFinalizedEventHandler.Handle(message, CancellationToken.None);
        _endpointInstance.SentMessages.Length.Should().Be(0);

        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>._, true)).MustNotHaveHappened();
    }

    [Fact]
    public async Task When_ProductCode_Is_Equals_DEE_Then_Evaluation_Should_HaveHappened()
    {
        var message = CreateMessageForTest();
        await _evaluationFinalizedEventHandler.Handle(message, CancellationToken.None);
        _endpointInstance.SentMessages.Length.Should().Be(1);

        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>._, true)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task When_DeeProductCodePresent_ThenMessageIsRoutedToEvalFinalized()
    {
        var message = CreateMessageForTest();

        var scopedHandler = new EvaluationFinalizedEventHandler(A.Dummy<ILogger<EvaluationFinalizedEventHandler>>(),
            _endpointInstance,
            _publishObservability);

        await scopedHandler.Handle(message, CancellationToken.None);
        _endpointInstance.SentMessages.Length.Should().Be(1);
        Assert.IsType<EvaluationFinalizedEvent>(_endpointInstance.SentMessages[0].Message);
    }

    public static EvaluationFinalizedEvent CreateMessageForTest()
    {
        return new EvaluationFinalizedEvent
        {
            ApplicationId = "Signify.Evaluation.Service",
            AppointmentId = 1,
            ClientId = 2,
            CreatedDateTime = ApplicationTime.UtcNow(),
            DateOfService = ApplicationTime.UtcNow(),
            DocumentPath = default,
            EvaluationId = 12,
            EvaluationTypeId = 2,
            FormVersionId = 3,
            Id = Guid.NewGuid(),
            Location = default,
            MemberId = 21,
            MemberPlanId = 21,
            Products = new List<Product> { new("DEE") },
            ProviderId = 21,
            ReceivedDateTime = DateTime.Now,
            UserName = "Caspar"
        };
    }
}