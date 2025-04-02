using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Refit;
using Signify.Dps.Observability.Library.Services;
using Signify.PAD.Svc.Core.ApiClient.Requests;
using Signify.PAD.Svc.Core.ApiClient.Response;
using Signify.PAD.Svc.Core.ApiClient;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Constants;
using Signify.PAD.Svc.Core.Data.Entities;
using Signify.PAD.Svc.Core.EventHandlers;
using Signify.PAD.Svc.Core.Exceptions;
using Signify.PAD.Svc.Core.Queries;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.EventHandlers;

public class ProviderPayHandlerTests
{
    private readonly IMapper _mapper;
    private readonly ProviderPayHandler _handler;
    private readonly IProviderPayApi _providerApi;
    private readonly ILogger<ProviderPayHandler> _logger;
    private readonly TestableInvokeHandlerContext _messageSession;
    private readonly IMediator _mediator;
    private readonly IObservabilityService _observabilityService;


    public ProviderPayHandlerTests()
    {
        _messageSession = new TestableInvokeHandlerContext();
        _logger = A.Fake<ILogger<ProviderPayHandler>>();
        _mapper = A.Fake<IMapper>();
        _providerApi = A.Fake<IProviderPayApi>();
        _mediator = A.Fake<IMediator>();
        _observabilityService = A.Fake<IObservabilityService>();

        _handler = new ProviderPayHandler(_logger, _mapper, _providerApi, _mediator, _observabilityService);
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
        A.CallTo(() => _mediator.Send(A<GetProviderPayByPadId>._, CancellationToken.None)).Returns(default(ProviderPay));

        await _handler.Handle(A.Dummy<ProviderPayRequest>(), _messageSession);

        A.CallTo(_logger)
            .Where(call => call.Method.Name == "Log" && call.GetArgument<LogLevel>(0) == LogLevel.Information)
            .MustHaveHappened(2, Times.OrMore);
        A.CallTo(_logger).Where(call => call.Method.Name == "Log" && call.GetArgument<LogLevel>(0) == LogLevel.Error)
            .MustNotHaveHappened();
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
        A.CallTo(() => _mediator.Send(A<GetProviderPayByPadId>._, CancellationToken.None)).Returns(default(ProviderPay));

        await Assert.ThrowsAsync<ProviderPayRequestException>(async () => await _handler.Handle(A.Dummy<ProviderPayRequest>(), _messageSession));

        A.CallTo(_logger)
            .Where(call => call.Method.Name == "Log" && call.GetArgument<LogLevel>(0) == LogLevel.Information)
            .MustHaveHappened(2, Times.OrMore);
        A.CallTo(_logger).Where(call => call.Method.Name == "Log" && call.GetArgument<LogLevel>(0) == LogLevel.Error)
            .MustHaveHappened();
        A.CallTo(() => _providerApi.SendProviderPayRequest(A<ProviderPayApiRequest>._))
            .MustHaveHappenedOnceExactly();
        Assert.Empty(_messageSession.SentMessages);
    }

    [Fact]
    public async Task Handler_Raise_SaveProviderPayEvent_When_ProviderPayAbsentInDb_And_ApiSuccess303()
    {
        var apiResponse = new ApiResponse<ProviderPayApiResponse>(new HttpResponseMessage(HttpStatusCode.SeeOther),
            A.Dummy<ProviderPayApiResponse>(), null!);
        A.CallTo(() => _mapper.Map<ProviderPayApiRequest>(A<ProviderPayRequest>._))
            .Returns(A.Dummy<ProviderPayApiRequest>());
        A.CallTo(() => _providerApi.SendProviderPayRequest(A<ProviderPayApiRequest>._)).Returns(apiResponse);
        A.CallTo(() => _mediator.Send(A<GetProviderPayByPadId>._, CancellationToken.None)).Returns(default(ProviderPay));

        await _handler.Handle(A.Dummy<ProviderPayRequest>(), _messageSession);

        A.CallTo(_logger)
            .Where(call => call.Method.Name == "Log" && call.GetArgument<LogLevel>(0) == LogLevel.Information)
            .MustHaveHappened(2, Times.OrMore);
        A.CallTo(_logger).Where(call => call.Method.Name == "Log" && call.GetArgument<LogLevel>(0) == LogLevel.Error)
            .MustNotHaveHappened();
        A.CallTo(() => _providerApi.SendProviderPayRequest(A<ProviderPayApiRequest>._))
            .MustHaveHappenedOnceExactly();
        Assert.Single(_messageSession.SentMessages);
    }

    [Fact]
    public async Task Handler_DoesNot_Raise_SaveProviderPayEvent_Nor_CallAPI_When_ProviderPayAlreadyPresentInDb()
    {
        var apiResponse = new ApiResponse<ProviderPayApiResponse>(new HttpResponseMessage(HttpStatusCode.SeeOther),
            A.Dummy<ProviderPayApiResponse>(), null!);
        A.CallTo(() => _mapper.Map<ProviderPayApiRequest>(A<ProviderPayRequest>._))
            .Returns(A.Dummy<ProviderPayApiRequest>());
        A.CallTo(() => _providerApi.SendProviderPayRequest(A<ProviderPayApiRequest>._)).Returns(apiResponse);
        A.CallTo(() => _mediator.Send(A<GetProviderPayByPadId>._, CancellationToken.None)).Returns(A.Dummy<ProviderPay>());

        await _handler.Handle(A.Dummy<ProviderPayRequest>(), _messageSession);

        A.CallTo(_logger)
            .Where(call => call.Method.Name == "Log" && call.GetArgument<LogLevel>(0) == LogLevel.Information)
            .MustHaveHappened(2, Times.OrMore);
        A.CallTo(_logger).Where(call => call.Method.Name == "Log" && call.GetArgument<LogLevel>(0) == LogLevel.Error)
            .MustNotHaveHappened();
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
        A.CallTo(() => _mediator.Send(A<GetProviderPayByPadId>._, CancellationToken.None)).Returns(default(ProviderPay));

        await Assert.ThrowsAsync<ProviderPayRequestException>(async () =>
            await _handler.Handle(A.Dummy<ProviderPayRequest>(), _messageSession));

        A.CallTo(_logger).Where(call => call.Method.Name == "Log" && call.GetArgument<LogLevel>(0) == LogLevel.Error)
            .MustHaveHappened();
        A.CallTo(() => _providerApi.SendProviderPayRequest(A<ProviderPayApiRequest>._))
            .MustHaveHappenedOnceExactly();
        Assert.Empty(_messageSession.SentMessages);
        A.CallTo(() => _observabilityService.AddEvent(Observability.ProviderPay.ProviderPayApiStatusCodeEvent, A<Dictionary<string, object>>._))
            .MustHaveHappenedOnceExactly(); 
        A.CallTo(() => _observabilityService.AddEvent(Observability.ProviderPay.ProviderPayOrBillingEvent, A<Dictionary<string, object>>._))
            .MustNotHaveHappened();
    }
}