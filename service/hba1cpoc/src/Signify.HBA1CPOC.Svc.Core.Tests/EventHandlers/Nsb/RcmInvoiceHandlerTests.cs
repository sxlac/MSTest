using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NServiceBus.Testing;
using Refit;
using Signify.HBA1CPOC.Messages.Events.Status;
using Signify.HBA1CPOC.Messages.Events;
using Signify.HBA1CPOC.Svc.Core.ApiClient.Requests;
using Signify.HBA1CPOC.Svc.Core.ApiClient;
using Signify.HBA1CPOC.Svc.Core.Commands;
using Signify.HBA1CPOC.Svc.Core.Constants;
using Signify.HBA1CPOC.Svc.Core.Data.Entities;
using Signify.HBA1CPOC.Svc.Core.EventHandlers;
using Signify.HBA1CPOC.Svc.Core.Exceptions;
using Signify.HBA1CPOC.Svc.Core.Infrastructure.Observability;
using Signify.HBA1CPOC.Svc.Core.Infrastructure;
using Signify.HBA1CPOC.Svc.Core.Queries;
using Signify.HBA1CPOC.Svc.Core.Tests.Mocks.StaticEntity;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.HBA1CPOC.Svc.Core.Tests.EventHandlers.Nsb;

public class RcmInvoiceHandlerTests
{
    private readonly IApplicationTime _applicationTime = new FakeApplicationTime();
    private readonly FakeTransactionSupplier _transactionSupplier = new();
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly RCMInvoiceHandler _rcmInvoiceHandler;
    private readonly TestableMessageHandlerContext _messageHandlerContext = new();
    private readonly IRcmApi _rcmApi = A.Fake<IRcmApi>();
    private readonly IApiResponse<Guid?> _fakeResponse = A.Fake<IApiResponse<Guid?>>();
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();

    public RcmInvoiceHandlerTests()
    {
        var logger = A.Dummy<ILogger<RCMInvoiceHandler>>();

        _rcmInvoiceHandler = new RCMInvoiceHandler(logger, _applicationTime, _transactionSupplier, _rcmApi, _mapper, _mediator, _publishObservability);

        A.CallTo(() => _rcmApi.SendBillingRequest(A<CreateBillRequest>._))
            .Returns(Task.FromResult(_fakeResponse));
    }

    [Fact]
    public async Task RcmInvoiceHandler_HaveExistingRcmBillingRecord_ShouldNotSendBillRequest()
    {
        // Arrange
        var message = GetRcmBillingRequest;
        A.CallTo(() => _mediator.Send(A<GetRcmBilling>._, A<CancellationToken>._)).Returns(new HBA1CPOCRCMBilling 
        { 
            Id = 1,
            BillId = Guid.NewGuid().ToString(),
            HBA1CPOCId = StaticMockEntities.Hba1Cpoc.HBA1CPOCId,
            CreatedDateTime = DateTime.UtcNow
        });

        // Act
        await _rcmInvoiceHandler.Handle(message, _messageHandlerContext);

        // Assert
        A.CallTo(() => _rcmApi.SendBillingRequest(A<CreateBillRequest>._)).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<GetHBA1CPOC>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<CreateHBA1CPOCStatus>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<CreateOrUpdateRCMBilling>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>._, A<CancellationToken>._)).MustNotHaveHappened();

        _transactionSupplier.AssertNoTransactionCreated();
    }

    [Fact]
    public async Task RCMInvoiceHandler_GetSuccessResponseFromRcm_PublishCheck()
    {
        // Arrange
        var @event = GetRcmBillingRequest;
        A.CallTo(() => _mediator.Send(A<GetRcmBilling>._, A<CancellationToken>._)).Returns((HBA1CPOCRCMBilling)null);
        A.CallTo(() => _mapper.Map<CreateBillRequest>(A<RCMBillingRequest>._)).Returns(new CreateBillRequest
        {
            ApplicationId = "signify.HBA1CPOC.svc",
            MemberPlanId = 21074285,
            DateOfService = DateTime.UtcNow,
            ProviderId = 42879,
            BillableDate = DateTime.UtcNow,
            RcmProductCode = ApplicationConstants.ProductCode,
            SharedClientId = 14,
            UsStateOfService = "US"
        });
        A.CallTo(() => _rcmApi.SendBillingRequest(A<CreateBillRequest>._)).Returns(GetSuccessfulRcmBillingResponse);
        A.CallTo(() => _mediator.Send(A<CreateHBA1CPOCStatus>._, CancellationToken.None)).Returns(default(HBA1CPOCStatus));
        A.CallTo(() => _mediator.Send(A<GetHBA1CPOC>._, CancellationToken.None)).Returns(StaticMockEntities.Hba1Cpoc);
        A.CallTo(() => _mediator.Send(A<CreateHBA1CPOCStatus>._, CancellationToken.None))
            .Returns(StaticMockEntities.CreateHbA1cPocStatus(HBA1CPOCStatusCode.BillRequestSent.HBA1CPOCStatusCodeId, HBA1CPOCStatusCode.BillRequestSent.StatusCodeName));

        // Act
        await _rcmInvoiceHandler.Handle(@event, _messageHandlerContext);

        // Assert
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>.That.Matches(u => u.Status is BillRequestSent), A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>.That.Matches(u => u.EventType == Observability.ProviderPay.RcmBillingApiStatusCodeEvent), true))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>.That.Matches(u => u.EventType == Observability.ProviderPay.ProviderPayOrBillingEvent), false))
            .MustHaveHappenedOnceExactly();

        _transactionSupplier.AssertCommit();
    }

    [Fact]
    public async Task RCMInvoiceHandler_GetEmptyBillId_DoNotPublishCheck()
    {
        // Arrange
        var @event = GetRcmBillingRequest;
        A.CallTo(() => _mediator.Send(A<GetRcmBilling>._, A<CancellationToken>._)).Returns((HBA1CPOCRCMBilling)null);
        A.CallTo(() => _mapper.Map<CreateBillRequest>(A<RCMBillingRequest>._)).Returns(new CreateBillRequest
        {
            ApplicationId = "signify.HBA1CPOC.svc",
            MemberPlanId = 21074285,
            DateOfService = DateTime.UtcNow,
            ProviderId = 42879,
            BillableDate = DateTime.UtcNow,
            RcmProductCode = ApplicationConstants.ProductCode,
            SharedClientId = 14,
            UsStateOfService = "US"
        });

        A.CallTo(() => _fakeResponse.IsSuccessStatusCode)
            .Returns(true);
        A.CallTo(() => _fakeResponse.Content)
            .Returns(default);

        // Act
        await Assert.ThrowsAsync<RcmBillingRequestException>(async () =>
            await _rcmInvoiceHandler.Handle(@event, _messageHandlerContext));

        // Assert
        A.CallTo(() => _mediator.Send(A<GetHBA1CPOC>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<CreateHBA1CPOCStatus>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<CreateOrUpdateRCMBilling>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>._, A<CancellationToken>._)).MustNotHaveHappened();

        _transactionSupplier.AssertNoTransactionCreated();
    }

    [Fact]
    public async Task RCMInvoiceHandler_GetBadRcmResponseWithConflict_DoNotPublishCheck()
    {
        // Arrange
        var @event = GetRcmBillingRequest;
        A.CallTo(() => _mediator.Send(A<GetRcmBilling>._, A<CancellationToken>._)).Returns((HBA1CPOCRCMBilling)null);
        A.CallTo(() => _mapper.Map<CreateBillRequest>(A<RCMBillingRequest>._)).Returns(new CreateBillRequest
        {
            ApplicationId = "signify.HBA1CPOC.svc",
            MemberPlanId = 21074285,
            DateOfService = DateTime.UtcNow,
            ProviderId = 42879,
            BillableDate = DateTime.UtcNow,
            RcmProductCode = ApplicationConstants.ProductCode,
            SharedClientId = 14,
            UsStateOfService = "US"
        });

        A.CallTo(() => _fakeResponse.IsSuccessStatusCode)
            .Returns(false);
        A.CallTo(() => _fakeResponse.StatusCode)
            .Returns(HttpStatusCode.MovedPermanently);
        A.CallTo(() => _fakeResponse.Error)
            .Returns(CreateApiError(Guid.NewGuid()));

        // Act
        await _rcmInvoiceHandler.Handle(@event, _messageHandlerContext);

        // Assert
        A.CallTo(() => _mediator.Send(A<GetHBA1CPOC>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<CreateHBA1CPOCStatus>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<CreateOrUpdateRCMBilling>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>._, A<CancellationToken>._)).MustHaveHappened();
    }

    [Fact]
    public async Task RCMInvoiceHandler_GetBadRcmResponseWithoutConflict_ThrowException()
    {
        // Arrange
        var @event = GetRcmBillingRequest;
        A.CallTo(() => _mediator.Send(A<GetRcmBilling>._, A<CancellationToken>._)).Returns((HBA1CPOCRCMBilling)null);
        A.CallTo(() => _mapper.Map<CreateBillRequest>(A<RCMBillingRequest>._)).Returns(new CreateBillRequest
        {
            ApplicationId = "signify.HBA1CPOC.svc",
            MemberPlanId = 21074285,
            DateOfService = DateTime.UtcNow,
            ProviderId = 42879,
            BillableDate = DateTime.UtcNow,
            RcmProductCode = ApplicationConstants.ProductCode,
            SharedClientId = 14,
            UsStateOfService = "US"
        });

        A.CallTo(() => _fakeResponse.IsSuccessStatusCode)
            .Returns(false);
        A.CallTo(() => _fakeResponse.StatusCode)
            .Returns(HttpStatusCode.BadRequest);

        // Act
        // Assert
        await Assert.ThrowsAsync<RcmBillingRequestException>(async () => await _rcmInvoiceHandler.Handle(@event, _messageHandlerContext));
    }

    private static RCMBillingRequest GetRcmBillingRequest => new()
    {
        EvaluationId = 324357,
        ApplicationId = "signify.hba1cpoc.svc",
        MemberPlanId = 21074285,
        DateOfService = DateTime.UtcNow,
        ProviderId = 42879,
        BillableDate = DateTime.UtcNow,
        Hba1cpocId = 2,
        RcmProductCode = ApplicationConstants.ProductCode,
        SharedClientId = 14,
        UsStateOfService = "TX"
    };

    private static ApiResponse<Guid?> GetSuccessfulRcmBillingResponse => new(
        new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            ReasonPhrase = string.Empty
        },
        Guid.NewGuid(),
        new RefitSettings()
    );

    private static ApiException CreateApiError(Guid billId)
        => new FakeApiException(HttpMethod.Post, HttpStatusCode.MovedPermanently, JsonConvert.SerializeObject(billId));
}
