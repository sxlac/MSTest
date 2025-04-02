using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Refit;
using Signify.CKD.Svc.Core.ApiClient;
using Signify.CKD.Svc.Core.ApiClient.Requests;
using Signify.CKD.Svc.Core.ApiClient.Response;
using Signify.CKD.Svc.Core.Commands;
using Signify.CKD.Svc.Core.Constants;
using Signify.CKD.Svc.Core.Data.Entities;
using Signify.CKD.Svc.Core.EventHandlers;
using Signify.CKD.Svc.Core.Exceptions;
using Signify.CKD.Svc.Core.Infrastructure.Observability;
using Signify.CKD.Svc.Core.Queries;
using Xunit;

namespace Signify.CKD.Svc.Core.Tests.EventHandlers;

public class ProviderPayRequestHandlerTest
{
    private readonly IMapper _mapper;
    private readonly ProviderPayRequestHandler _providerPayHandler;
    private readonly IProviderPayApi _providerApi;
    private readonly ILogger<ProviderPayRequestHandler> _logger;
    private readonly TestableInvokeHandlerContext _messageSession;
    private readonly IMediator _mediator;
    private readonly IObservabilityService _observabilityService;

    public ProviderPayRequestHandlerTest()
    {
        _messageSession = new TestableInvokeHandlerContext();
        _logger = A.Fake<ILogger<ProviderPayRequestHandler>>();
        _mapper = A.Fake<IMapper>();
        _providerApi = A.Fake<IProviderPayApi>();
        _mediator = A.Fake<IMediator>();
        _observabilityService = A.Fake<IObservabilityService>();
        
        _providerPayHandler = new ProviderPayRequestHandler(_logger, _mapper, _providerApi, _mediator, _observabilityService);
    }

    [Fact]
    public async Task Handler_Raise_SaveProviderPayEvent_When_ProviderPayAbsentInDb_And_ApiSuccess202()
    {
        var apiResponseBody = A.Fake<ProviderPayApiResponse>();
        apiResponseBody.PaymentId = Guid.NewGuid().ToString();
        var apiResponse = new ApiResponse<ProviderPayApiResponse>(new HttpResponseMessage(HttpStatusCode.Accepted),
            apiResponseBody, null!);
        A.CallTo(() => _mapper.Map<ProviderPayApiRequest>(A<ProviderPayRequest>._))
            .Returns(A.Dummy<ProviderPayApiRequest>());
        A.CallTo(() => _providerApi.SendProviderPayRequest(A<ProviderPayApiRequest>._)).Returns(apiResponse);
        A.CallTo(() => _mediator.Send(A<GetProviderPayByCkdId>._,CancellationToken.None)).Returns(default(ProviderPay));
        
        await _providerPayHandler.Handle(A.Dummy<ProviderPayRequest>(), _messageSession);

        A.CallTo(() => _providerApi.SendProviderPayRequest(A<ProviderPayApiRequest>._))
            .MustHaveHappenedOnceExactly();
        Assert.Single(_messageSession.SentMessages);
        A.CallTo(() => _observabilityService.AddEvent(Observability.ProviderPay.ProviderPayApiStatusCodeEvent, A<Dictionary<string, object>>._))
            .MustHaveHappenedOnceExactly(); 
        A.CallTo(() => _observabilityService.AddEvent(Observability.ProviderPay.ProviderPayOrBillingEvent, A<Dictionary<string, object>>._))
            .MustHaveHappenedOnceExactly();
    }
    
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Handler_ThrowException_When_ProviderPayAbsentInDb_And_ApiSuccess202_And_PaymentIdNullOrEmpty(string paymentId)
    {
        var apiResponseBody = A.Fake<ProviderPayApiResponse>();
        apiResponseBody.PaymentId = paymentId;
        var apiResponse = new ApiResponse<ProviderPayApiResponse>(new HttpResponseMessage(HttpStatusCode.Accepted),
            apiResponseBody, null!);
        A.CallTo(() => _mapper.Map<ProviderPayApiRequest>(A<ProviderPayRequest>._))
            .Returns(A.Dummy<ProviderPayApiRequest>());
        A.CallTo(() => _providerApi.SendProviderPayRequest(A<ProviderPayApiRequest>._)).Returns(apiResponse);
        A.CallTo(() => _mediator.Send(A<GetProviderPayByCkdId>._,CancellationToken.None)).Returns(default(ProviderPay));
        
        await Assert.ThrowsAsync<ProviderPayRequestException>(async ()=> await _providerPayHandler.Handle(A.Dummy<ProviderPayRequest>(), _messageSession));

        A.CallTo(() => _providerApi.SendProviderPayRequest(A<ProviderPayApiRequest>._))
            .MustHaveHappenedOnceExactly();
        Assert.Empty(_messageSession.SentMessages);
    }

    [Fact]
    public async Task Handler_Raise_SaveProviderPayEvent_When_ProviderPayAbsentInDb_And_ApiSuccess303ButNoPaymentIdReturned()
    {
        var apiResponse = new ApiResponse<ProviderPayApiResponse>(new HttpResponseMessage(HttpStatusCode.SeeOther),
            A.Dummy<ProviderPayApiResponse>(), null!);
        A.CallTo(() => _mapper.Map<ProviderPayApiRequest>(A<ProviderPayRequest>._))
            .Returns(A.Dummy<ProviderPayApiRequest>());
        A.CallTo(() => _providerApi.SendProviderPayRequest(A<ProviderPayApiRequest>._)).Returns(apiResponse);
        A.CallTo(() => _mediator.Send(A<GetProviderPayByCkdId>._,CancellationToken.None)).Returns(default(ProviderPay));

        await Assert.ThrowsAsync<ProviderPayRequestException>(async () =>
            await _providerPayHandler.Handle(A.Dummy<ProviderPayRequest>(), _messageSession));
    } 
    
    [Fact]
    public async Task Handler_DoesNot_Raise_SaveProviderPayEvent_Nor_CallAPI_When_ProviderPayAlreadyPresentInDb()
    {
        var apiResponse = new ApiResponse<ProviderPayApiResponse>(new HttpResponseMessage(HttpStatusCode.SeeOther),
            A.Dummy<ProviderPayApiResponse>(), null!);
        A.CallTo(() => _mapper.Map<ProviderPayApiRequest>(A<ProviderPayRequest>._))
            .Returns(A.Dummy<ProviderPayApiRequest>());
        A.CallTo(() => _providerApi.SendProviderPayRequest(A<ProviderPayApiRequest>._)).Returns(apiResponse);
        A.CallTo(() => _mediator.Send(A<GetProviderPayByCkdId>._,CancellationToken.None)).Returns(A.Dummy<ProviderPay>());

        await _providerPayHandler.Handle(A.Dummy<ProviderPayRequest>(), _messageSession);

        A.CallTo(() => _providerApi.SendProviderPayRequest(A<ProviderPayApiRequest>._))
            .MustNotHaveHappened();
        Assert.Empty(_messageSession.SentMessages);
        A.CallTo(() => _observabilityService.AddEvent(Observability.ProviderPay.ProviderPayApiStatusCodeEvent, A<Dictionary<string, object>>._))
            .MustNotHaveHappened(); 
        A.CallTo(() => _observabilityService.AddEvent(Observability.ProviderPay.ProviderPayOrBillingEvent, A<Dictionary<string, object>>._))
            .MustNotHaveHappened();
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task Handler_DoesNot_Raise_SaveProviderPayEvent_When_ApiFail(
        HttpStatusCode statusCode)
    {
        var apiResponse = new ApiResponse<ProviderPayApiResponse>(new HttpResponseMessage(statusCode),
            null, null!);
        A.CallTo(() => _mapper.Map<ProviderPayApiRequest>(A<ProviderPayRequest>._))
            .Returns(A.Dummy<ProviderPayApiRequest>());
        A.CallTo(() => _providerApi.SendProviderPayRequest(A<ProviderPayApiRequest>._)).Returns(apiResponse);
        A.CallTo(() => _mediator.Send(A<GetProviderPayByCkdId>._,CancellationToken.None)).Returns(default(ProviderPay));

        await Assert.ThrowsAsync<ProviderPayRequestException>(async () =>
            await _providerPayHandler.Handle(A.Dummy<ProviderPayRequest>(), _messageSession));

        A.CallTo(() => _providerApi.SendProviderPayRequest(A<ProviderPayApiRequest>._))
            .MustHaveHappenedOnceExactly();
        Assert.Empty(_messageSession.SentMessages);
        A.CallTo(() => _observabilityService.AddEvent(Observability.ProviderPay.ProviderPayApiStatusCodeEvent, A<Dictionary<string, object>>._))
            .MustHaveHappenedOnceExactly(); 
        A.CallTo(() => _observabilityService.AddEvent(Observability.ProviderPay.ProviderPayOrBillingEvent, A<Dictionary<string, object>>._))
            .MustNotHaveHappened();
    }
}