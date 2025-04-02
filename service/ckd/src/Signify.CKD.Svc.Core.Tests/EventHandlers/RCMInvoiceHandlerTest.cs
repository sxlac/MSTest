using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Refit;
using Signify.CKD.Messages.Events;
using Signify.CKD.Svc.Core.ApiClient;
using Signify.CKD.Svc.Core.ApiClient.Requests;
using Signify.CKD.Svc.Core.Commands;
using Signify.CKD.Svc.Core.Constants;
using Signify.CKD.Svc.Core.Data.Entities;
using Signify.CKD.Svc.Core.EventHandlers;
using Signify.CKD.Svc.Core.Exceptions;
using Signify.CKD.Svc.Core.Infrastructure.Observability;
using Signify.CKD.Svc.Core.Queries;
using Signify.CKD.Svc.Core.Tests.Data;
using Signify.CKD.Svc.Core.Tests.Utilities;
using Xunit;

namespace Signify.CKD.Svc.Core.Tests.EventHandlers;

public class RcmInvoiceHandlerTest : IClassFixture<MockDbFixture>
{
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly RCMInvoiceHandler _subject;
    private readonly IRcmApi _rcmApi = A.Fake<IRcmApi>();
    private readonly IApiResponse<Guid?> _fakeResponse = A.Fake<IApiResponse<Guid?>>();
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly IObservabilityService _observabilityService = A.Fake<IObservabilityService>();
    private readonly FakeTransactionSupplier _transactionSupplier = new();
    
    public RcmInvoiceHandlerTest(MockDbFixture mockDbFixture)
    {
        _subject = new RCMInvoiceHandler(A.Dummy<ILogger<RCMInvoiceHandler>>(), _rcmApi, _mapper, _transactionSupplier, _mediator, _observabilityService);

        A.CallTo(() => _mediator.Send(A<GetCKD>._, A<CancellationToken>._))
            .ReturnsLazily(call =>
            {
                var query = call.Arguments.Get<GetCKD>(0);
                return Task.FromResult(mockDbFixture.Context.CKD.FirstOrDefault(each => each.EvaluationId == query.EvaluationId));
            });

        A.CallTo(() => _rcmApi.SendBillingRequest(A<RCMBilling>._))
            .Returns(Task.FromResult(_fakeResponse));
    }

    [Fact]
    public async Task RCMInvoiceHandler_PublishCheck()
    {
        var @event = GetRcmBillingRequest;
        A.CallTo(() => _mapper.Map<RCMBilling>(A<RCMBillingRequest>._)).Returns(new RCMBilling
        {
            ApplicationId = "signify.ckd.svc",
            MemberPlanId = 21074285,
            DateOfService = DateTime.UtcNow,
            ProviderId = 42879,
            BillableDate = DateTime.UtcNow,
            RcmProductCode = Application.ProductCode,
            SharedClientId = 14,
            UsStateOfService = "US"
        });
        A.CallTo(() => _rcmApi.SendBillingRequest(A<RCMBilling>._)).Returns(GetRcmBillingResponse());
        A.CallTo(() => _mediator.Send(A<CreateCKDStatus>._, CancellationToken.None)).Returns(default(CKDStatus));
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdateHandler>._, A<CancellationToken>._)).Returns(Unit.Value);

        await _subject.Handle(@event, default);
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>._, default)).MustNotHaveHappened();
    }

    private static RCMBillingRequest GetRcmBillingRequest => new RCMBillingRequest
    {
        EvaluationId = 324357,
        ApplicationId = "signify.ckd.svc",
        MemberPlanId = 21074285,
        DateOfService = DateTime.UtcNow,
        ProviderId = 42879,
        BillableDate = DateTime.UtcNow,
        CKDId = 2,
        RcmProductCode = Application.ProductCode,
        SharedClientId = 14,
        UsStateOfService = "US"
    };

    private static ApiResponse<Guid?> GetRcmBillingResponse()
    {
        var billId = Guid.Empty;
        var httpResponseMessage = new HttpResponseMessage
        {
            Content = ContentHelper.GetStringContent(billId)
        };

        return new ApiResponse<Guid?>(httpResponseMessage, billId, new RefitSettings());
    }

    [Fact]
    public async Task Handle_WhenBillAlreadyExists_DoesNothing()
    {
        // Arrange
        const int ckdId = 1;

        var request = new RCMBillingRequest
        {
            CKDId = ckdId
        };

        A.CallTo(() => _mediator.Send(A<GetRcmBilling>._, A<CancellationToken>._))
            .Returns(Task.FromResult(new CKDRCMBilling()));

        // Act
        await _subject.Handle(request, default);

        // Assert
        A.CallTo(() => _mediator.Send(A<GetRcmBilling>.That.Matches(q => q.CKDId == ckdId),
                A<CancellationToken>._))
            .MustHaveHappened();

        A.CallTo(_mapper)
            .MustNotHaveHappened();
        A.CallTo(_rcmApi)
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task Handle_WithUnsuccessfulStatusCode_Throws()
    {
        // Arrange
        const int evaluationId = 1;

        var request = new RCMBillingRequest
        {
            EvaluationId = evaluationId
        };

        A.CallTo(() => _mediator.Send(A<GetRcmBilling>._, A<CancellationToken>._))
            .Returns(Task.FromResult<CKDRCMBilling>(null));

        A.CallTo(() => _fakeResponse.IsSuccessStatusCode)
            .Returns(false);
        A.CallTo(() => _fakeResponse.StatusCode)
            .Returns(HttpStatusCode.Unauthorized); // Really just anything other than 200-level or 301

        // Act/Assert
        await Assert.ThrowsAnyAsync<RcmBillingRequestException>(async () => await _subject.Handle(request, default));

        A.CallTo(() => _rcmApi.SendBillingRequest(A<RCMBilling>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _observabilityService.AddEvent(A<string>.That.Matches(s => s == "DpsBillRequestFailed"),
                A<Dictionary<string, object>>._))
            .MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<CreateCKDStatus>._, A<CancellationToken>._))
            .MustNotHaveHappened();

        _transactionSupplier.AssertNoTransactionCreated();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_HappyPath(bool isMovedPermanently)
    {
        // Arrange
        const int evaluationId = 10;
        const int ckdId = 11;
        var billId = Guid.NewGuid();

        var request = new RCMBillingRequest
        {
            EvaluationId = evaluationId
        };

        A.CallTo(() => _mediator.Send(A<GetRcmBilling>._, A<CancellationToken>._))
            .Returns(Task.FromResult<CKDRCMBilling>(null));

        A.CallTo(() => _mediator.Send(A<GetCKD>._, A<CancellationToken>._))
            .Returns(new Core.Data.Entities.CKD
            {
                CKDId = ckdId
            });

        A.CallTo(() => _fakeResponse.IsSuccessStatusCode)
            .Returns(!isMovedPermanently);
        A.CallTo(() => _fakeResponse.StatusCode)
            .Returns(isMovedPermanently ? HttpStatusCode.MovedPermanently : HttpStatusCode.Accepted);

        // 202 puts BillId on the response content
        A.CallTo(() => _fakeResponse.Content)
            .Returns(!isMovedPermanently ? billId : null);
        // 301 puts BillId on the error content
        A.CallTo(() => _fakeResponse.Error)
            .Returns(isMovedPermanently ? CreateApiError(billId) : null);

        // Act
        await _subject.Handle(request, default);

        // Assert
        A.CallTo(() => _mediator.Send(A<GetCKD>
                    .That.Matches(q => q.EvaluationId == evaluationId),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mapper.Map<RCMBilling>(A<RCMBillingRequest>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _rcmApi.SendBillingRequest(A<RCMBilling>._))
            .MustHaveHappenedOnceExactly();

        _transactionSupplier.AssertCommit();
    }

    private static ApiException CreateApiError(Guid billId)
        => new FakeApiException(HttpMethod.Post, HttpStatusCode.MovedPermanently, JsonConvert.SerializeObject(billId));
}
