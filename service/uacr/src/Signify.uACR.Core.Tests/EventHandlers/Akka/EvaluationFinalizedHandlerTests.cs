using AutoMapper;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Signify.uACR.Core.EventHandlers.Akka;
using Signify.uACR.Core.Events.Akka;
using Signify.uACR.Core.Filters;
using UacrNsbEvents;
using System.Collections.Generic;
using System.Threading.Tasks;
using Signify.Dps.Observability.Library.Services;
using Xunit;

namespace Signify.uACR.Core.Tests.EventHandlers.Akka;

public class EvaluationFinalizedHandlerTests11
{
    private readonly FakeApplicationTime _fakeApplicationTime = new();
    private readonly IMapper _fakeMapper = A.Fake<IMapper>();
    private readonly IMessageSession _fakeMessageSession = A.Fake<IMessageSession>();
    private readonly IProductFilter _productFilter = A.Fake<IProductFilter>();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();

    private EvaluationFinalizedHandler CreateSubject()
        => new(A.Dummy<ILogger<EvaluationFinalizedHandler>>(),
            _fakeMapper, _fakeMessageSession, _fakeApplicationTime, _productFilter, _publishObservability);

    private void ValidateAllCallsExactly(int times)
    {
        A.CallTo(_fakeMapper).MustHaveHappened(times, Times.Exactly);
        A.CallTo(_fakeMessageSession).MustHaveHappened(times, Times.Exactly);
    }

    private void SetupFilter(bool shouldProcess)
    {
        A.CallTo(() => _productFilter.ShouldProcess(A<ICollection<Product>>._))
            .Returns(shouldProcess);
    }

    [Fact]
    public async Task Handle_WithNonUacrProductCodes_DoesNothing()
    {
        var e = new EvaluationFinalizedEvent();

        SetupFilter(false);

        var subject = CreateSubject();

        await subject.Handle(e, default);

        ValidateAllCallsExactly(0);
    }

    [Fact]
    public async Task Handle_WithUacrProductCode_IsHandled()
    {
        var e = new EvaluationFinalizedEvent();

        SetupFilter(true);

        var mappedEvalReceived = new EvalReceived();

        var subject = CreateSubject();

        A.CallTo(() => _fakeMapper.Map<EvalReceived>(A<EvaluationFinalizedEvent>.That.IsSameAs(e)))
            .Returns(mappedEvalReceived);

        await subject.Handle(e, default);

        ValidateAllCallsExactly(1);

        A.CallTo(_fakeMessageSession)
            .MustHaveHappened(1, Times.Exactly);
    }
}