using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Refit;
using Signify.DEE.Core.Messages.Queries;
using Signify.DEE.Svc.Core.ApiClients.CdiApi.Holds;
using Signify.DEE.Svc.Core.ApiClients.CdiApi.Holds.Requests;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.EventHandlers.Nsb;
using Signify.DEE.Svc.Core.Exceptions;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.EventHandlers.NSB
{
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

        private ReleaseHoldHandler CreateSubject() => new(A.Dummy<ILogger<ReleaseHoldHandler>>(), _cdiHoldsApi);

        [Fact]
        public async Task Handle_HappyPath()
        {
            // Arrange
            var cdiHoldId = Guid.NewGuid();
            var request = new ReleaseHold(new Hold() { EvaluationId = 1, CdiHoldId = cdiHoldId, ReleasedDateTime = null });

            A.CallTo(() => _fakeResponse.IsSuccessStatusCode)
                .Returns(true);

            // Act
            await CreateSubject().Handle(request, default);

            // Assert

            A.CallTo(() => _cdiHoldsApi.ReleaseHold(A<Guid>.That.Matches(g => g == cdiHoldId), A<ReleaseHoldRequest>.That.Matches(r =>
                    r.ApplicationId == "Signify.DEE.Svc" &&
                    r.ProductCodes.Count() == 1 &&
                    r.ProductCodes.First() == "DEE")))
                .MustHaveHappened();

            A.CallTo(() => _fakeResponse.IsSuccessStatusCode)
                .MustHaveHappened();
        }

        [Fact]
        public async Task Handle_WhenAlreadyReleased_DoesNothing()
        {
            // Arrange
            var request = new ReleaseHold(new Hold() { ReleasedDateTime = DateTime.UtcNow });

            // Act
            await CreateSubject().Handle(request, default);

            // Assert
            A.CallTo(() => _cdiHoldsApi.ReleaseHold(A<Guid>._, A<ReleaseHoldRequest>._))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task Handle_WhenCdiApiRespondsUnsuccessfully_Throws()
        {
            // Arrange
            var request = new ReleaseHold(new Hold());

            A.CallTo(() => _mediator.Send(A<GetHold>._, A<CancellationToken>._))
                .Returns(new Hold
                {
                    ReleasedDateTime = null
                });

            A.CallTo(() => _fakeResponse.IsSuccessStatusCode)
                .Returns(false);

            // Assert
            await Assert.ThrowsAsync<ReleaseHoldRequestException>(async () => await CreateSubject().Handle(request, default));
        }
    }
}
