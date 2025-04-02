using AutoMapper;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.eGFR.Core.EventHandlers.Akka;
using Signify.eGFR.Core.Events.Akka;
using Signify.eGFR.Core.Filters;
using EgfrNsbEvents;
using System.Collections.Generic;
using System.Threading.Tasks;
using NServiceBus.Testing;
using Signify.Dps.Observability.Library.Services;
using Xunit;

namespace Signify.eGFR.Core.Tests.EventHandlers.Akka;

public class EvaluationFinalizedHandlerTests
{
    private readonly FakeApplicationTime _fakeApplicationTime = new();
    private readonly IMapper _fakeMapper = A.Fake<IMapper>();
    private readonly TestableEndpointInstance _fakeMessageSession = new();
    private readonly IProductFilter _productFilter = A.Fake<IProductFilter>();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();
    private EvaluationFinalizedHandler CreateSubject()
    {
        return new EvaluationFinalizedHandler(A.Dummy<ILogger<EvaluationFinalizedHandler>>(),
            _fakeMapper, _fakeMessageSession, _fakeApplicationTime, _productFilter, _publishObservability);
    }

    private void ValidateAllCallsExactly(int times)
    {
        A.CallTo(_fakeMapper).MustHaveHappened(times, Times.Exactly);
        Assert.Equal(times,_fakeMessageSession.SentMessages.Length);

    }

    private void SetupFilter(bool shouldProcess)
    {
        A.CallTo(() => _productFilter.ShouldProcess(A<ICollection<Product>>._))
            .Returns(shouldProcess);
    }

    [Fact]
    public async Task Handle_WithNoneGFRProductCodes_DoesNothing()
    {
        var e = new EvaluationFinalizedEvent();

        SetupFilter(false);

        var subject = CreateSubject();

        await subject.Handle(e, default);

        ValidateAllCallsExactly(0);
    }

    [Fact]
    public async Task Handle_WitheGFRProductCode_IsHandled()
    {
        var e = new EvaluationFinalizedEvent();

        SetupFilter(true);

        var mappedEvalReceived = new EvalReceived();

        var subject = CreateSubject();

        A.CallTo(() => _fakeMapper.Map<EvalReceived>(A<EvaluationFinalizedEvent>.That.IsSameAs(e)))
            .Returns(mappedEvalReceived);

        await subject.Handle(e, default);

        ValidateAllCallsExactly(1);
        Assert.NotNull(_fakeMessageSession.FindSentMessage<EvalReceived>());
    }
}