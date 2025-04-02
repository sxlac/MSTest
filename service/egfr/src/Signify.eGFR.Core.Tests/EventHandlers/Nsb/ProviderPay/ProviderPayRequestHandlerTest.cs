using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NsbEventHandlers;
using NServiceBus.Testing;
using Refit;
using Signify.Dps.Observability.Library.Services;
using Signify.eGFR.Core.ApiClients.ProviderPayApi;
using Signify.eGFR.Core.ApiClients.ProviderPayApi.Requests;
using Signify.eGFR.Core.ApiClients.ProviderPayApi.Responses;
using Signify.eGFR.Core.BusinessRules;
using Signify.eGFR.Core.Commands;
using Signify.eGFR.Core.Exceptions;
using Signify.eGFR.Core.Models;
using Signify.eGFR.Core.Queries;
using Xunit;

namespace Signify.eGFR.Core.Tests.EventHandlers.Nsb.ProviderPay;

public class ProviderPayRequestHandlerTest
{
    private readonly IMapper _mapper;
    private readonly ProviderPayRequestHandler _providerPayHandler;
    private readonly IProviderPayApi _providerPayApi;
    private readonly ILogger<ProviderPayRequestHandler> _logger;
    private readonly TestableMessageHandlerContext _messageSession;
    private readonly IMediator _mediator;
    private readonly IPublishObservability _publishObservability;
    private readonly FakeTransactionSupplier _transactionSupplier = new();
    private readonly IPayableRules _payableRules = A.Fake<IPayableRules>();

    public ProviderPayRequestHandlerTest()
    {
        _messageSession = new TestableMessageHandlerContext();
        _logger = A.Fake<ILogger<ProviderPayRequestHandler>>();
        _mapper = A.Fake<IMapper>();
        _providerPayApi = A.Fake<IProviderPayApi>();
        _mediator = A.Fake<IMediator>();
        _publishObservability = A.Fake<IPublishObservability>();
        _providerPayHandler =
            new ProviderPayRequestHandler(_logger, _mediator, _transactionSupplier, _publishObservability, new FakeApplicationTime(), _mapper, _providerPayApi,   _payableRules);
    }

    [Fact]
    public async Task Handler_Raise_SaveProviderPayEvent_When_ProviderPayAbsentInDb_And_ApiSuccess202()
    {
        var apiResponseBody = A.Fake<ProviderPayApiResponse>();
        apiResponseBody.PaymentId = Guid.NewGuid().ToString();
        var apiResponse = new ApiResponse<ProviderPayApiResponse>(new HttpResponseMessage(HttpStatusCode.Accepted),
            apiResponseBody, null!);
        var exam = A.Fake<Data.Entities.Exam>();
        exam.EvaluationId = 1;
        A.CallTo(() => _mapper.Map<ProviderPayApiRequest>(A<ProviderPayRequest>._))
            .Returns(A.Dummy<ProviderPayApiRequest>());
        A.CallTo(() => _providerPayApi.SendProviderPayRequest(A<ProviderPayApiRequest>._)).Returns(apiResponse);
        A.CallTo(() => _mediator.Send(A<QueryProviderPayByExamId>._, CancellationToken.None)).Returns(default(Data.Entities.ProviderPay));
        A.CallTo(() => _payableRules.IsPayable(A<PayableRuleAnswers>._))
            .Returns(new BusinessRuleStatus { IsMet = true });

        await _providerPayHandler.Handle(A.Dummy<ProviderPayRequest>(), _messageSession);

        A.CallTo(() => _providerPayApi.SendProviderPayRequest(A<ProviderPayApiRequest>._))
            .MustHaveHappenedOnceExactly();
        Assert.Single(_messageSession.SentMessages);
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
        A.CallTo(() => _providerPayApi.SendProviderPayRequest(A<ProviderPayApiRequest>._)).Returns(apiResponse);
        A.CallTo(() => _mediator.Send(A<QueryProviderPayByExamId>._, CancellationToken.None)).Returns(default(Data.Entities.ProviderPay));
        A.CallTo(() => _payableRules.IsPayable(A<PayableRuleAnswers>._))
            .Returns(new BusinessRuleStatus { IsMet = true });

        await Assert.ThrowsAsync<ProviderPayRequestException>(async () => await _providerPayHandler.Handle(A.Dummy<ProviderPayRequest>(), _messageSession));

        A.CallTo(() => _providerPayApi.SendProviderPayRequest(A<ProviderPayApiRequest>._))
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
        A.CallTo(() => _providerPayApi.SendProviderPayRequest(A<ProviderPayApiRequest>._)).Returns(apiResponse);
        A.CallTo(() => _mediator.Send(A<QueryProviderPayByExamId>._, CancellationToken.None)).Returns(default(Data.Entities.ProviderPay));
        A.CallTo(() => _payableRules.IsPayable(A<PayableRuleAnswers>._))
            .Returns(new BusinessRuleStatus { IsMet = true });

        await Assert.ThrowsAsync<ProviderPayRequestException>(async () =>
            await _providerPayHandler.Handle(A.Dummy<ProviderPayRequest>(), _messageSession));

        A.CallTo(() => _providerPayApi.SendProviderPayRequest(A<ProviderPayApiRequest>._))
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
        A.CallTo(() => _providerPayApi.SendProviderPayRequest(A<ProviderPayApiRequest>._)).Returns(apiResponse);
        A.CallTo(() => _mediator.Send(A<QueryProviderPayByExamId>._,CancellationToken.None)).Returns(A.Dummy<Data.Entities.ProviderPay>());
        A.CallTo(() => _payableRules.IsPayable(A<PayableRuleAnswers>._))
            .Returns(new BusinessRuleStatus{IsMet = true});
    
        await _providerPayHandler.Handle(A.Dummy<ProviderPayRequest>(), _messageSession);
    
        A.CallTo(() => _providerPayApi.SendProviderPayRequest(A<ProviderPayApiRequest>._))
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
        A.CallTo(() => _providerPayApi.SendProviderPayRequest(A<ProviderPayApiRequest>._)).Returns(apiResponse);
        A.CallTo(() => _mediator.Send(A<QueryProviderPayByExamId>._,CancellationToken.None)).Returns(default(Data.Entities.ProviderPay));
        A.CallTo(() => _payableRules.IsPayable(A<PayableRuleAnswers>._))
            .Returns(new BusinessRuleStatus{IsMet = true});
        
        await Assert.ThrowsAsync<ProviderPayRequestException>(async () =>
            await _providerPayHandler.Handle(A.Dummy<ProviderPayRequest>(), _messageSession));
    
        A.CallTo(() => _providerPayApi.SendProviderPayRequest(A<ProviderPayApiRequest>._))
            .MustHaveHappenedOnceExactly();
        Assert.Empty(_messageSession.SentMessages);
    }
}