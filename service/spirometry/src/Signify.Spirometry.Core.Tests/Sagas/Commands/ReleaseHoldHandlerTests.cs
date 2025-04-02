using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Refit;
using Signify.Spirometry.Core.ApiClients.CdiApi.Holds.Requests;
using Signify.Spirometry.Core.ApiClients.CdiApi.Holds;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Exceptions;
using Signify.Spirometry.Core.Queries;
using SpiroNsb.SagaCommands;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Sagas.Commands;

public class ReleaseHoldHandlerTests
{
    private readonly IApiResponse _fakeResponse = A.Fake<IApiResponse>();
    private readonly ICdiHoldsApi _cdiHoldsApi = A.Fake<ICdiHoldsApi>();
    private readonly IMediator _mediator = A.Fake<IMediator>();

    public ReleaseHoldHandlerTests()
    {
        A.CallTo(() => _cdiHoldsApi.ReleaseHold(A<Guid>._, A<ReleaseHoldRequest>._))
            .Returns(_fakeResponse);
        A.CallTo(() => _fakeResponse.Error)
            .Returns(new FakeApiException(HttpMethod.Put, HttpStatusCode.NotFound));
    }

    private ReleaseHoldHandler CreateSubject() => new(A.Dummy<ILogger<ReleaseHoldHandler>>(), _cdiHoldsApi, _mediator);

    [Fact]
    public async Task Handle_WhenAlreadyReleased_DoesNothing()
    {
        // Arrange
        const long evaluationId = 1;

        var request = new ReleaseHold(evaluationId, default);

        A.CallTo(() => _mediator.Send(A<QueryHold>._, A<CancellationToken>._))
            .Returns(new Hold
            {
                ReleasedDateTime = DateTime.UtcNow
            });

        // Act
        await CreateSubject().Handle(request, new TestableMessageHandlerContext());

        // Assert
        A.CallTo(() => _mediator.Send(A<QueryHold>.That.Matches(q =>
                    q.EvaluationId == evaluationId),
                A<CancellationToken>._))
            .MustHaveHappened();

        A.CallTo(_cdiHoldsApi)
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task Handle_WhenCdiApiRespondsUnsuccessfully_Throws()
    {
        // Arrange
        const int evaluationId = 1;
        const int holdId = 2;

        var request = new ReleaseHold(evaluationId, holdId);

        A.CallTo(() => _mediator.Send(A<QueryHold>._, A<CancellationToken>._))
            .Returns(new Hold
            {
                ReleasedDateTime = null
            });

        A.CallTo(() => _fakeResponse.IsSuccessStatusCode)
            .Returns(false);

        // Act
        // Assert
        await Assert.ThrowsAsync<ReleaseHoldRequestException>(async () => await CreateSubject().Handle(request, new TestableMessageHandlerContext()));
    }

    [Fact]
    public async Task Handle_HappyPath()
    {
        // Arrange
        const long evaluationId = 1;
        const int holdId = 2;
        var cdiHoldId = Guid.NewGuid();

        var request = new ReleaseHold(evaluationId, holdId);

        A.CallTo(() => _mediator.Send(A<QueryHold>._, A<CancellationToken>._))
            .Returns(new Hold
            {
                CdiHoldId = cdiHoldId,
                ReleasedDateTime = null
            });

        A.CallTo(() => _fakeResponse.IsSuccessStatusCode)
            .Returns(true);

        // Act
        await CreateSubject().Handle(request, new TestableMessageHandlerContext());

        // Assert
        A.CallTo(() => _mediator.Send(A<QueryHold>.That.Matches(q =>
                    q.EvaluationId == evaluationId),
                A<CancellationToken>._))
            .MustHaveHappened();

        A.CallTo(() => _cdiHoldsApi.ReleaseHold(A<Guid>.That.Matches(g => g == cdiHoldId), A<ReleaseHoldRequest>.That.Matches(r =>
                r.ApplicationId == "Signify.Spirometry.Svc" &&
                r.ProductCodes.Count() == 1 &&
                r.ProductCodes.First() == "SPIROMETRY")))
            .MustHaveHappened();

        A.CallTo(() => _fakeResponse.IsSuccessStatusCode)
            .MustHaveHappened();
    }
}