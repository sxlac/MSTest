using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Refit;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using Signify.PAD.Svc.Core.ApiClient.Requests;
using Signify.PAD.Svc.Core.ApiClient;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Constants;
using Signify.PAD.Svc.Core.Data.Entities;
using Signify.PAD.Svc.Core.Data;
using Signify.PAD.Svc.Core.EventHandlers;
using Signify.PAD.Svc.Core.Events;
using Signify.PAD.Svc.Core.Exceptions;
using Signify.PAD.Svc.Core.Infrastructure;
using Signify.PAD.Svc.Core.Queries;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.EventHandlers;

public class RcmInvoiceHandlerTests
{
    private readonly RcmInvoiceHandler _handler;
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly TestableMessageHandlerContext _messageHandlerContext;
    private readonly IRcmApi _rcmApi = A.Fake<IRcmApi>();
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly ITransactionSupplier _transactionSupplier = A.Fake<ITransactionSupplier>();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();
    private readonly IApiResponse<Guid?> _fakeResponse = A.Fake<IApiResponse<Guid?>>();
    private readonly IApplicationTime _applicationTime = A.Fake<IApplicationTime>();

    public RcmInvoiceHandlerTests()
    {
        var logger = A.Fake<ILogger<RcmInvoiceHandler>>();
        _messageHandlerContext = new TestableMessageHandlerContext();
        _handler = new RcmInvoiceHandler(logger, _rcmApi, _mapper, _mediator, _transactionSupplier, _applicationTime, _publishObservability);
        A.CallTo(() => _rcmApi.SendBillingRequest(A<CreateBillRequest>._))
            .Returns(Task.FromResult(_fakeResponse));
    }

    [Fact]
    public async Task Handle_RcmBillingRequest_AlreadyProcessedBillingRequest()
    {
        var @event = GetRcmBillingRequest;

        A.CallTo(() => _mediator.Send(A<QueryBillRequestSent>.That.Matches(x => x.PadId == @event.PadId), A<CancellationToken>._))
            .Returns(true);

        await _handler.Handle(@event, _messageHandlerContext);

        A.CallTo(() => _mapper.Map<CreateBillRequest>(@event)).MustNotHaveHappened();
    }

    [Fact]
    public async Task Handle_RcmBillingRequest_NeverProcessed()
    {
        var @event = GetRcmBillingRequest;

        A.CallTo(() => _mediator.Send(A<QueryBillRequestSent>.That.Matches(x => x.PadId == @event.PadId), A<CancellationToken>._))
            .Returns(false);

        A.CallTo(() => _rcmApi.SendBillingRequest(A<CreateBillRequest>._))
            .Returns(Task.FromResult(CreateUnsuccessfulApiResponse<Guid?>()));

        await Assert.ThrowsAsync<RcmBillingRequestException>(() => _handler.Handle(@event, _messageHandlerContext));

        A.CallTo(() => _mapper.Map<CreateBillRequest>(@event)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task BillingRequest_Sends_RcmBillingRequest()
    {
        //Arrange
        var @event = GetRcmBillingRequest;

        var pad = new Core.Data.Entities.PAD
        {
            PADId = 1,
            EvaluationId = @event.EvaluationId
        };

        var billId = Guid.NewGuid();

        A.CallTo(() => _mediator.Send(A<QueryBillRequestSent>.That.Matches(x => x.PadId == @event.PadId), A<CancellationToken>._))
            .Returns(false);
        A.CallTo(() => _mediator.Send(A<GetPAD>.That.Matches(x => x.EvaluationId == @event.EvaluationId),
            A<CancellationToken>._)).Returns(pad);
        A.CallTo(() => _rcmApi.SendBillingRequest(A<CreateBillRequest>._))
            .Returns(Task.FromResult(CreateApiResponse<Guid?>(Guid.NewGuid())));
        A.CallTo(() => _mapper.Map(A<Core.Data.Entities.PAD>._, A<BillRequestSent>._))
            .Returns(new BillRequestSent() { BillId = billId.ToString(), EvaluationId = @event.EvaluationId });

        //Act
        await _handler.Handle(@event, _messageHandlerContext);

        //Assert
        A.CallTo(() => _mapper.Map<CreateBillRequest>(@event)).MustHaveHappenedOnceExactly();

        A.CallTo(() =>
                _rcmApi.SendBillingRequest(A<CreateBillRequest>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _mediator.Send(A<GetPAD>.That.Matches(x => x.EvaluationId == pad.EvaluationId),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _mediator.Send(A<CreateOrUpdateRcmBilling>.That.Matches(x => x.RcmBilling.PADId == pad.PADId),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _mediator.Send(A<CreatePadStatus>.That.Matches(x => x.StatusCode == PADStatusCode.BillRequestSent),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _mediator.Send(A<BillRequestSent>.That.Matches(x => x.EvaluationId == pad.EvaluationId),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>.That.Matches(u => u.EventType == Observability.ProviderPay.ProviderPayOrBillingEvent), 
                false))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>.That.Matches(u => u.EventType == Observability.ProviderPay.RcmBillingApiStatusCodeEvent), 
                true))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task BillingRequest_Sends_ConflictedRcmBillingRequest()
    {
        //Arrange
        var @event = GetRcmBillingRequest;

        var pad = new Core.Data.Entities.PAD
        {
            PADId = 1,
            EvaluationId = @event.EvaluationId
        };

        A.CallTo(() => _mediator.Send(A<QueryBillRequestSent>.That.Matches(x => x.PadId == @event.PadId), A<CancellationToken>._))
            .Returns(false);
        A.CallTo(() => _rcmApi.SendBillingRequest(A<CreateBillRequest>._))
            .Returns(Task.FromResult(CreateUnsuccessfulApiResponse<Guid?>("Conflict")));

        //Act
        await _handler.Handle(@event, _messageHandlerContext);

        //Assert
        A.CallTo(() => _mapper.Map<CreateBillRequest>(@event)).MustHaveHappenedOnceExactly();

        A.CallTo(() =>
                _rcmApi.SendBillingRequest(A<CreateBillRequest>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _mediator.Send(A<GetPAD>.That.Matches(x => x.EvaluationId == pad.EvaluationId),
                A<CancellationToken>._))
            .MustNotHaveHappened();

        A.CallTo(() => _mediator.Send(A<CreateOrUpdateRcmBilling>.That.Matches(x => x.RcmBilling.PADId == pad.PADId),
                A<CancellationToken>._))
            .MustNotHaveHappened();

        A.CallTo(() => _mediator.Send(A<CreatePadStatus>.That.Matches(x => x.StatusCode == PADStatusCode.BillRequestSent),
                A<CancellationToken>._))
            .MustNotHaveHappened();

        A.CallTo(() => _mediator.Send(A<BillRequestSent>.That.Matches(x => x.EvaluationId == pad.EvaluationId),
                A<CancellationToken>._))
            .MustNotHaveHappened();

        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>._, false)).MustNotHaveHappened();
        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>.That.Matches(u => u.EventType == Observability.ProviderPay.RcmBillingApiStatusCodeEvent), true))
            .MustHaveHappenedOnceExactly();
    }

    private static RcmBillingRequest GetRcmBillingRequest => new()
    {
        EvaluationId = 324357,
        ApplicationId = "signify.pad.svc",
        MemberPlanId = 21074285,
        DateOfService = DateTime.UtcNow,
        ProviderId = 42879,
        BillableDate = DateTime.UtcNow,
        PadId = 2,
        RcmProductCode = Application.ProductCode,
        SharedClientId = 14,
        UsStateOfService = "US"
    };

    private static IApiResponse<T> CreateUnsuccessfulApiResponse<T>(string reasonPhrase = default)
    {
        var apiResponse = A.Fake<IApiResponse<T>>();

        A.CallTo(() => apiResponse.IsSuccessStatusCode)
            .Returns(false);

        if (!string.IsNullOrEmpty(reasonPhrase))
        {
            A.CallTo(() => apiResponse.ReasonPhrase)
                .Returns(reasonPhrase);
        }

        return apiResponse;
    }

    /// <summary>
    /// Creates a successful response with the included content
    /// </summary>
    private static IApiResponse<T> CreateApiResponse<T>(T content)
    {
        var apiResponse = A.Fake<IApiResponse<T>>();

        A.CallTo(() => apiResponse.IsSuccessStatusCode)
            .Returns(true);

        A.CallTo(() => apiResponse.Content)
            .Returns(content);

        return apiResponse;
    }
}