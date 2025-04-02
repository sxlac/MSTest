using AutoMapper;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using Signify.Spirometry.Core.EventHandlers.Akka;
using Signify.Spirometry.Core.Events.Akka;
using Signify.Spirometry.Core.Filters;
using SpiroNsbEvents;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.Spirometry.Core.Tests.EventHandlers.Akka;

public class EvaluationFinalizedHandlerTests
{
    private readonly FakeApplicationTime _fakeApplicationTime = new();
    private readonly IMapper _fakeMapper = A.Fake<IMapper>();
    private readonly IEndpointInstance _fakeEndpoint = A.Fake<IEndpointInstance>();
    private readonly IProductFilter _productFilter = A.Fake<IProductFilter>();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();
        
    private EvaluationFinalizedHandler CreateSubject()
    {
        return new EvaluationFinalizedHandler(A.Dummy<ILogger<EvaluationFinalizedHandler>>(),
            _fakeMapper, _fakeEndpoint, _fakeApplicationTime, _productFilter, _publishObservability);
    }

    private void ValidateAllCallsExactly(int times)
    {
        A.CallTo(_fakeMapper).MustHaveHappened(times, Times.Exactly);
        A.CallTo(_fakeEndpoint).MustHaveHappened(times, Times.Exactly);
    }

    private void SetupFilter(bool shouldProcess)
    {
        A.CallTo(() => _productFilter.ShouldProcess(A<ICollection<Product>>._))
            .Returns(shouldProcess);
    }

    [Fact]
    public async Task Handle_WithNonSpiroProductCodes_DoesNothing()
    {
        var e = new EvaluationFinalizedEvent();

        SetupFilter(false);

        var subject = CreateSubject();

        await subject.Handle(e, default);

        ValidateAllCallsExactly(0);
            
        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>._, true)).MustNotHaveHappened();
    }

    [Fact]
    public async Task Handle_WithSpiroProductCode_IsHandled()
    {
        var e = new EvaluationFinalizedEvent();

        SetupFilter(true);

        var mappedEvalReceived = new EvalReceived();

        var subject = CreateSubject();

        A.CallTo(() => _fakeMapper.Map<EvalReceived>(A<EvaluationFinalizedEvent>.That.IsSameAs(e)))
            .Returns(mappedEvalReceived);

        await subject.Handle(e, default);

        ValidateAllCallsExactly(1);

        A.CallTo(() => _fakeEndpoint.Send(A<EvalReceived>.That.Matches(
                evalReceived => evalReceived == mappedEvalReceived && evalReceived.ReceivedBySpirometryProcessManagerDateTime == _fakeApplicationTime.UtcNow()), A<SendOptions>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>._, true)).MustHaveHappenedOnceExactly();
    }
}