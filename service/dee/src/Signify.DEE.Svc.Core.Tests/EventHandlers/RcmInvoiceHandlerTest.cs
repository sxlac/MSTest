using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Refit;
using Signify.AkkaStreams.Kafka.Persistence;
using Signify.DEE.Svc.Core.ApiClient;
using Signify.DEE.Svc.Core.ApiClient.Requests;
using Signify.DEE.Svc.Core.ApiClient.Responses;
using Signify.DEE.Svc.Core.Commands;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.EventHandlers;
using Signify.DEE.Svc.Core.Events;
using Signify.DEE.Svc.Core.Exceptions;
using Signify.DEE.Svc.Core.Messages.Commands;
using Signify.DEE.Svc.Core.Messages.Models;
using Signify.DEE.Svc.Core.Messages.Queries;
using Signify.DEE.Svc.Core.Tests.Utilities;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Signify.DEE.Svc.Core.Constants;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.EventHandlers;

public class RcmInvoiceHandlerTest
{
    private readonly IMapper _mapper;
    private readonly RcmInvoiceHandler _rcmInvoiceHandler;
    private readonly TestableMessageHandlerContext _messageHandlerContext;
    private readonly IRCMApi _rcmApi;
    private readonly IMediator _mediator;
    private static readonly FakeApplicationTime ApplicationTime = new();
    private readonly ITransactionSupplier _transactionSupplier;
    private readonly IPublishObservability _publishObservability;

    public RcmInvoiceHandlerTest()
    {
        _mapper = A.Fake<IMapper>();
        _rcmApi = A.Fake<IRCMApi>();
        _mediator = A.Fake<IMediator>();
        _transactionSupplier = A.Fake<ITransactionSupplier>();
        _publishObservability = A.Fake<IPublishObservability>();
        _messageHandlerContext = new TestableMessageHandlerContext();
        _rcmInvoiceHandler = new RcmInvoiceHandler(A.Dummy<ILogger<RcmInvoiceHandler>>(), _rcmApi, _mapper, _mediator, ApplicationTime,
            _transactionSupplier, _publishObservability);
    }

    [Fact]
    public async Task When_Called_RCMInvoiceHandler_Should_Invoke_BillRequestSentHandler_When_Rcm_Response_Is_Success()
    {
        // Arrange
        var @event = GetRcmBillingRequest;
        SetupFakeBillingResponse();

        A.CallTo(() => _rcmApi.SendRCMRequestForBilling(A<RCMBilling>._)).Returns(GetRcmBillingResponse());

        A.CallTo(() => _mediator.Send(A<GetEvalAnswers>._, A<CancellationToken>._)).Returns(new ExamAnswersModel
            { Answers = [new EvaluationAnswer()]});

        var transaction = A.Fake<IBufferedTransaction>();
        A.CallTo(() => _transactionSupplier.BeginTransaction()).Returns(transaction);

        // Act
        await _rcmInvoiceHandler.Handle(@event, _messageHandlerContext);

        // Assert
        A.CallTo(() => _mediator.Send(A<CreateStatus>.That.Matches(
                s => s.ExamStatusCode.Equals(ExamStatusCode.SentToBilling)), A<CancellationToken>._)
        ).MustHaveHappenedOnceExactly();

        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>.That.Matches(p => p.Status is DEE.Messages.Status.BillRequestSent), A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _transactionSupplier.BeginTransaction()).MustHaveHappened();
        A.CallTo(() => transaction.CommitAsync(A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => transaction.Dispose()).MustHaveHappened();
        A.CallTo(() => _publishObservability.RegisterEvent(
                A<ObservabilityEvent>.That.Matches(a => a.EventType == Observability.ProviderPay.RcmBillingApiStatusCodeEvent), true))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _publishObservability.RegisterEvent(
                A<ObservabilityEvent>.That.Matches(a => a.EventType == Observability.ProviderPay.ProviderPayOrBillingEvent), false))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _publishObservability.Commit()).MustHaveHappenedOnceExactly();

        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>._, true)).MustHaveHappenedTwiceExactly();
    }

    [Fact]
    public async Task When_Called_RCMInvoiceHandler_Should_Not_Invoke_BillRequestSentHandler_When_Rcm_Response_Is_Not_Success()
    {
        // Arrange
        var @event = GetRcmBillingRequest;
        SetupFakeBillingResponse();

        var billId = Guid.NewGuid();
        var httpResponseMessage = new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.BadRequest
        };

        A.CallTo(() => _rcmApi.SendRCMRequestForBilling(A<RCMBilling>._)).Returns(new ApiResponse<Guid?>(httpResponseMessage, billId, new RefitSettings()));

        // Act & Assert
        await Assert.ThrowsAsync<RcmBillingException>(async () => await _rcmInvoiceHandler.Handle(@event, _messageHandlerContext));

        A.CallTo(() => _mediator.Send(A<CreateStatus>.That.Matches(
                s => s.ExamStatusCode.Equals(ExamStatusCode.SentToBilling)), A<CancellationToken>._)
        ).MustNotHaveHappened();

        A.CallTo(() => _mediator.Send(A<BillRequestSent>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _publishObservability.RegisterEvent(
                A<ObservabilityEvent>.That.Matches(a => a.EventType == Observability.ProviderPay.RcmBillingApiStatusCodeEvent), true))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _publishObservability.RegisterEvent(
                A<ObservabilityEvent>.That.Matches(a => a.EventType == Observability.ProviderPay.ProviderPayOrBillingEvent), false))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _publishObservability.Commit()).MustNotHaveHappened();

        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>._, true)).MustHaveHappenedTwiceExactly();
    }

    private void SetupFakeBillingResponse()
    {
        A.CallTo(() => _mapper.Map<RCMBilling>(A<RCMBillingRequestEvent>._)).Returns(new RCMBilling
        {
            ApplicationId = "signify.DEE.svc",
            MemberPlanId = 21074285,
            DateOfService = ApplicationTime.UtcNow(),
            ProviderId = 42879,
            BillableDate = ApplicationTime.UtcNow(),
            RcmProductCode = ApplicationConstants.ProductCode,
            SharedClientId = 14,
            UsStateOfService = "US"
        });
    }

    private static RCMBillingRequestEvent GetRcmBillingRequest => new()
    {
        EvaluationId = 324357,
        ApplicationId = "signify.DEE.svc",
        ProviderId = 42879,
        BillableDate = ApplicationTime.UtcNow(),
        RcmProductCode = ApplicationConstants.ProductCode,
        SharedClientId = 14,
        AppointmentId = 12345,
    };

    private static ApiResponse<Guid?> GetRcmBillingResponse()
    {
        var billId = Guid.NewGuid();
        var httpResponseMessage = new HttpResponseMessage
        {
            Content = ContentHelper.GetStringContent(billId)
        };

        return new ApiResponse<Guid?>(httpResponseMessage, billId, new RefitSettings());
    }
}