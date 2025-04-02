using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Refit;
using Signify.HBA1CPOC.Svc.Core.ApiClient.Requests;
using Signify.HBA1CPOC.Svc.Core.ApiClient.Response;
using Signify.HBA1CPOC.Svc.Core.ApiClient;
using Signify.HBA1CPOC.Svc.Core.Commands;
using Signify.HBA1CPOC.Svc.Core.Constants;
using Signify.HBA1CPOC.Svc.Core.Data.Entities;
using Signify.HBA1CPOC.Svc.Core.EventHandlers;
using Signify.HBA1CPOC.Svc.Core.Exceptions;
using Signify.HBA1CPOC.Svc.Core.Infrastructure.Observability;
using Signify.HBA1CPOC.Svc.Core.Queries;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.HBA1CPOC.Svc.Core.Tests.EventHandlers.Nsb;

public class ProviderPayRequestHandlerTest
{
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly ProviderPayRequestHandler _providerPayHandler;
    private readonly IProviderPayApi _providerApi = A.Fake<IProviderPayApi>();
    private readonly TestableInvokeHandlerContext _messageSession = new();
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();

    public ProviderPayRequestHandlerTest()
    {
        var logger = A.Dummy<ILogger<ProviderPayRequestHandler>>();

        _providerPayHandler = new ProviderPayRequestHandler(logger, _mapper, _providerApi, _mediator, _publishObservability);
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
        A.CallTo(() => _mediator.Send(A<GetProviderPayByHba1CpocId>._,CancellationToken.None)).Returns(default(ProviderPay));
        
        await _providerPayHandler.Handle(A.Dummy<ProviderPayRequest>(), _messageSession);

        A.CallTo(() => _providerApi.SendProviderPayRequest(A<ProviderPayApiRequest>._))
            .MustHaveHappenedOnceExactly();
        Assert.Single(_messageSession.SentMessages);
        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>.That.Matches(u => u.EventType == Observability.ProviderPay.ProviderPayApiStatusCodeEvent), true))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>.That.Matches(u => u.EventType == Observability.ProviderPay.ProviderPayOrBillingEvent), false))
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
        A.CallTo(() => _mediator.Send(A<GetProviderPayByHba1CpocId>._,CancellationToken.None)).Returns(default(ProviderPay));
        
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
        A.CallTo(() => _mediator.Send(A<GetProviderPayByHba1CpocId>._,CancellationToken.None)).Returns(default(ProviderPay));

        await Assert.ThrowsAsync<ProviderPayRequestException>(async () =>
            await _providerPayHandler.Handle(A.Dummy<ProviderPayRequest>(), _messageSession));

        A.CallTo(() => _providerApi.SendProviderPayRequest(A<ProviderPayApiRequest>._))
            .MustHaveHappenedOnceExactly();
        Assert.Empty(_messageSession.SentMessages);
    } 

    [Fact]
    public async Task Handler_DoesNot_Raise_SaveProviderPayEvent_Nor_CallAPI_When_ProviderPayAlreadyPresentInDb()
    {
        var apiResponse = new ApiResponse<ProviderPayApiResponse>(new HttpResponseMessage(HttpStatusCode.SeeOther),
            A.Dummy<ProviderPayApiResponse>(), null!);
        A.CallTo(() => _mapper.Map<ProviderPayApiRequest>(A<ProviderPayRequest>._))
            .Returns(A.Dummy<ProviderPayApiRequest>());
        A.CallTo(() => _providerApi.SendProviderPayRequest(A<ProviderPayApiRequest>._)).Returns(apiResponse);
        A.CallTo(() => _mediator.Send(A<GetProviderPayByHba1CpocId>._,CancellationToken.None)).Returns(A.Dummy<ProviderPay>());

        await _providerPayHandler.Handle(A.Dummy<ProviderPayRequest>(), _messageSession);

        A.CallTo(() => _providerApi.SendProviderPayRequest(A<ProviderPayApiRequest>._))
            .MustNotHaveHappened();
        Assert.Empty(_messageSession.SentMessages);
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
        A.CallTo(() => _mediator.Send(A<GetProviderPayByHba1CpocId>._,CancellationToken.None)).Returns(default(ProviderPay));

        await Assert.ThrowsAsync<ProviderPayRequestException>(async () =>
            await _providerPayHandler.Handle(A.Dummy<ProviderPayRequest>(), _messageSession));

        A.CallTo(() => _providerApi.SendProviderPayRequest(A<ProviderPayApiRequest>._))
            .MustHaveHappenedOnceExactly();
        Assert.Empty(_messageSession.SentMessages);
    }
}
