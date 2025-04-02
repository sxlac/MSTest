using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Refit;
using Signify.FOBT.Messages.Events.Status;
using Signify.FOBT.Svc.Core.ApiClient.Requests;
using Signify.FOBT.Svc.Core.ApiClient;
using Signify.FOBT.Svc.Core.Commands;
using Signify.FOBT.Svc.Core.Data.Entities;
using Signify.FOBT.Svc.Core.EventHandlers;
using Signify.FOBT.Svc.Core.Events;
using Signify.FOBT.Svc.Core.Exceptions;
using Signify.FOBT.Svc.Core.Infrastructure.Observability;
using Signify.FOBT.Svc.Core.Queries;
using Signify.FOBT.Svc.Core.Tests.Mocks.Models;
using Signify.FOBT.Svc.Core.Tests.Utilities;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System;
using Xunit;
using Fobt = Signify.FOBT.Svc.Core.Data.Entities.FOBT;

namespace Signify.FOBT.Svc.Core.Tests.EventHandlers;

public class RcmRequestEventHandlerTests : IClassFixture<MockDbFixture>
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly IRcmApi _rcmApi;
    private readonly IMessageHandlerContext _fakeContext;
    private readonly RcmRequestEventHandler _handler;

    public RcmRequestEventHandlerTests(MockDbFixture mockDbFixture)
    {
        var logger = A.Fake<ILogger<RcmRequestEventHandler>>();
        _rcmApi = A.Fake<IRcmApi>();
        _mediator = A.Fake<IMediator>();
        _mapper = A.Fake<IMapper>();
        _fakeContext = A.Fake<IMessageHandlerContext>();
        var publishObservability = A.Fake<IPublishObservability>();

        _handler = new RcmRequestEventHandler(logger, mockDbFixture.Context, _mediator, _mapper, _rcmApi, publishObservability);
    }

    [Fact]
    public async Task RcmRequestEventHandler_WhenBillAlreadyExistsForFobtId_SkipsSendingRequestForNewBill()
    {
        // Arrange
        A.CallTo(() => _mediator.Send(A<GetFobtBilling>._, default)).Returns(FobtBillingMock.BuildFobtBilling());

        // Act
        await _handler.Handle(RcmRequestEvent, _fakeContext);

        // Assert
        A.CallTo(() => _mediator.Send(A<CreateOrUpdateRcmBilling>._, default)).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<CreateFOBTStatus>._, default)).MustNotHaveHappened();
        A.CallTo(() => _mapper.Map<BillRequestSent>(A<Fobt>._)).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>._, default)).MustNotHaveHappened();
    }

    [Fact]
    public async Task RcmRequestEventHandler_GetBadRequestResponse_ThrowApplicationException()
    {
        // Arrange
        A.CallTo(() => _mediator.Send(A<GetFobtBilling>._, default)).Returns((FOBTBilling)null);
        A.CallTo(() => _mapper.Map<RCMBilling>(A<RCMRequestEvent>._)).Returns(RcmBilling);
        A.CallTo(() => _rcmApi.SendRcmRequestForBilling(A<RCMBilling>._)).Returns(BadResponse);

        // Act
        // Assert
        await Assert.ThrowsAsync<RcmBillingException>(async () => await _handler.Handle(RcmRequestEvent, _fakeContext));
    }

    [Fact]
    public async Task RcmRequestEventHandler_GetBadRequestResponseWithConflictReason_OnlyLogError()
    {
        // Arrange
        A.CallTo(() => _mediator.Send(A<GetFobtBilling>._, default)).Returns((FOBTBilling)null);
        A.CallTo(() => _mapper.Map<RCMBilling>(A<RCMRequestEvent>._)).Returns(RcmBilling);
        A.CallTo(() => _rcmApi.SendRcmRequestForBilling(A<RCMBilling>._)).Returns(BadResponseForConflict);

        // Act
        await _handler.Handle(RcmRequestEvent, _fakeContext);

        // Assert
        A.CallTo(() => _mediator.Send(A<CreateOrUpdateRcmBilling>._, default)).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<CreateFOBTStatus>._, default)).MustNotHaveHappened();
        A.CallTo(() => _mapper.Map<BillRequestSent>(A<Fobt>._)).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>._, default)).MustNotHaveHappened();
    }

    [Fact]
    public async Task RcmRequestEventHandler_GetSuccessStatusCodeButNoBillId_LogError()
    {
        // Arrange
        A.CallTo(() => _mediator.Send(A<GetFobtBilling>._, default)).Returns((FOBTBilling)null);
        A.CallTo(() => _mapper.Map<RCMBilling>(A<RCMRequestEvent>._)).Returns(RcmBilling);
        A.CallTo(() => _rcmApi.SendRcmRequestForBilling(A<RCMBilling>._)).Returns(SuccessResponseWithoutBillId);

        // Act
        await _handler.Handle(RcmRequestEvent, _fakeContext);

        // Assert
        A.CallTo(() => _mediator.Send(A<CreateOrUpdateRcmBilling>._, default)).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<CreateFOBTStatus>._, default)).MustNotHaveHappened();
        A.CallTo(() => _mapper.Map<BillRequestSent>(A<Fobt>._)).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>._, default)).MustNotHaveHappened();
    }

    [Fact]
    public async Task RcmRequestEventHandler_GetSuccessStatusCodeWithBillId_SendBillRequest()
    {
        // Arrange
        A.CallTo(() => _mediator.Send(A<GetFobtBilling>._, default)).Returns((FOBTBilling)null);
        A.CallTo(() => _mapper.Map<RCMBilling>(A<RCMRequestEvent>._)).Returns(RcmBilling);
        A.CallTo(() => _rcmApi.SendRcmRequestForBilling(A<RCMBilling>._)).Returns(SuccessResponseWithBillId);
        A.CallTo(() => _mediator.Send(A<CreateFOBTStatus>._, default)).Returns(FobtStatusEntityMock.BuildFobtStatus(1, FOBTStatusCode.BillRequestSent));
        A.CallTo(() => _mapper.Map<BillRequestSent>(A<Fobt>._)).Returns(new BillRequestSent());

        // Act
        await _handler.Handle(RcmRequestEvent, _fakeContext);

        // Assert
        A.CallTo(() => _mediator.Send(A<CreateOrUpdateRcmBilling>._, default)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<CreateFOBTStatus>._, default)).MustHaveHappened();
        A.CallTo(() => _mapper.Map<BillRequestSent>(A<Fobt>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>._, default)).MustHaveHappened();
    }

    private static RCMRequestEvent RcmRequestEvent => new()
    { 
        FOBT = FobtEntityMock.BuildFobt(1),
        FOBTId = 1,
        BillingProductCode = "1"
    };

    private static RCMBilling RcmBilling => new()
    { 
        SharedClientId = 1,
        MemberPlanId = 1,
        DateOfService = DateTime.UtcNow.AddDays(-1),
        ProviderId = 1,
        RcmProductCode = "FOBT-Results",
        ApplicationId = "1",
        BillableDate = DateTime.UtcNow,
        CorrelationId = Guid.NewGuid().ToString()
    };

    private static ApiResponse<Guid?> BadResponse => new(
        new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest },
        Guid.NewGuid(),
        new RefitSettings()
    );

    private static ApiResponse<Guid?> BadResponseForConflict => new(
        new HttpResponseMessage
        { 
            StatusCode = HttpStatusCode.BadRequest,
            ReasonPhrase = "conflict"
        },
        Guid.NewGuid(),
        new RefitSettings()
    );

    private static ApiResponse<Guid?> SuccessResponseWithoutBillId => new(
        new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            ReasonPhrase = string.Empty
        },
        null,
        new RefitSettings()
    );

    private static ApiResponse<Guid?> SuccessResponseWithBillId => new(
        new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            ReasonPhrase = string.Empty
        },
        Guid.NewGuid(),
        new RefitSettings()
    );
}