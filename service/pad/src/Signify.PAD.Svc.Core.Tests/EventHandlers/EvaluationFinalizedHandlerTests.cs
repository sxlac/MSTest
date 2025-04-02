using AutoMapper;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.Dps.Observability.Library.Services;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.EventHandlers;
using Signify.PAD.Svc.Core.Events;
using Signify.PAD.Svc.Core.Filters;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.EventHandlers;

public class EvaluationFinalizedHandlerTests
{
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly TestableEndpointInstance _endpoint = new();
    private readonly IProductFilter _productFilter = A.Fake<IProductFilter>();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();
    private EvaluationFinalizedHandler CreateSubject()
        => new(A.Dummy<ILogger<EvaluationFinalizedHandler>>(), _endpoint, _mapper, _productFilter, _publishObservability);

    private void ValidateAllCallsExactly(int times)
    {
        A.CallTo(_mapper).MustHaveHappened(times, Times.Exactly);
        Assert.Equal(times, _endpoint.SentMessages.Length);
    }

    private void SetupFilter(bool shouldProcess)
    {
        A.CallTo(() => _productFilter.ShouldProcess(A<ICollection<Product>>._))
            .Returns(shouldProcess);
    }

    [Fact]
    public async Task Handle_WithNonPadProductCodes_DoesNothing()
    {
        var e = new EvaluationFinalizedEvent();

        SetupFilter(false);

        var subject = CreateSubject();

        await subject.Handle(e, default);

        ValidateAllCallsExactly(0);
    }

    [Fact]
    public async Task Handle_WithPadProductCode_IsHandled()
    {
        var e = new EvaluationFinalizedEvent();

        SetupFilter(true);

        var subject = CreateSubject();

        await subject.Handle(e, default);

        ValidateAllCallsExactly(1);

        var message = _endpoint.FindSentMessage<CreatePad>();
        Assert.NotNull(message);
    }
}