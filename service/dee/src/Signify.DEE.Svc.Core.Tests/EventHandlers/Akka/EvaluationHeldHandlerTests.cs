using FakeItEasy;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Signify.DEE.Svc.Core.EventHandlers.Akka;
using Signify.DEE.Svc.Core.Events;
using Signify.DEE.Svc.Core.Filters;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.EventHandlers.Akka;

public class EvaluationHeldHandlerTests
{
    private readonly IEndpointInstance _fakeEndpoint = A.Fake<IEndpointInstance>();
    private readonly IProductFilter _productFilter = A.Fake<IProductFilter>();

    private EvaluationHeldHandler CreateSubject()
    {
        return new EvaluationHeldHandler(A.Dummy<ILogger<EvaluationHeldHandler>>(),
            _fakeEndpoint, _productFilter);
    }

    private void ValidateAllCallsExactly(int times)
    {
        A.CallTo(_fakeEndpoint).MustHaveHappened(times, Times.Exactly);
    }

    private void SetupFilter(bool shouldProcess)
    {
        A.CallTo(() => _productFilter.ShouldProcess(A<ICollection<ProductHold>>._))
            .Returns(shouldProcess);
    }

    [Fact]
    public async Task Handle_HoldCreatedWithNonDeeProductCodes_DoesNothing()
    {
        var e = new CDIEvaluationHeldEvent();

        SetupFilter(false);

        var subject = CreateSubject();

        await subject.Handle(e, default);

        ValidateAllCallsExactly(0);
    }

    [Fact]
    public async Task Handle_HoldCreatedWithDeeProductCode_IsHandled()
    {
        var e = new CDIEvaluationHeldEvent();

        SetupFilter(true);

        var subject = CreateSubject();

        await subject.Handle(e, default);

        A.CallTo(() => _fakeEndpoint.Send(A<CDIEvaluationHeldEvent>.That.Matches(
                holdCreated => holdCreated == e), A<SendOptions>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }
}