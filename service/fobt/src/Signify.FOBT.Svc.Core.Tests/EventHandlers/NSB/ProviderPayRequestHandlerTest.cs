using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NsbEventHandlers;
using NServiceBus.Testing;
using Refit;
using Signify.FOBT.Svc.Core.ApiClient.Requests;
using Signify.FOBT.Svc.Core.ApiClient.Response;
using Signify.FOBT.Svc.Core.ApiClient;
using Signify.FOBT.Svc.Core.BusinessRules;
using Signify.FOBT.Svc.Core.Commands;
using Signify.FOBT.Svc.Core.Data.Entities;
using Signify.FOBT.Svc.Core.Data;
using Signify.FOBT.Svc.Core.Exceptions;
using Signify.FOBT.Svc.Core.Infrastructure.Observability;
using Signify.FOBT.Svc.Core.Models;
using Signify.FOBT.Svc.Core.Queries;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.EventHandlers.NSB;

public class ProviderPayRequestHandlerTest
{
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly ProviderPayHandler _handler;
    private readonly IProviderPayApi _providerApi = A.Fake<IProviderPayApi>();
    private readonly TestableInvokeHandlerContext _messageSession = new();
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly IPayableRules _payableRules = A.Fake<IPayableRules>();

    public ProviderPayRequestHandlerTest()
    {
        var logger = A.Dummy<ILogger<ProviderPayHandler>>();

        _handler = new ProviderPayHandler(logger, _mapper, _providerApi, _mediator, A.Fake<ITransactionSupplier>(), A.Fake<IPublishObservability>(),_payableRules);
    }

    [Fact]
    public async Task Handler_Raise_SaveProviderPayEvent_When_ProviderPayAbsentInDb_And_ApiSuccess202()
    {
        var apiResponseBody = A.Fake<ProviderPayApiResponse>();
        apiResponseBody.PaymentId = Guid.NewGuid().ToString();
        var apiResponse = new ApiResponse<ProviderPayApiResponse>(new HttpResponseMessage(HttpStatusCode.Accepted),
            apiResponseBody, null!);
        var exam = A.Fake<Core.Data.Entities.FOBT>();
        exam.EvaluationId = 1;
        A.CallTo(() => _mediator.Send(A<GetFOBT>._, A<CancellationToken>._)).Returns(exam);
        A.CallTo(() => _mapper.Map<ProviderPayApiRequest>(A<ProviderPayRequest>._))
            .Returns(A.Dummy<ProviderPayApiRequest>());
        A.CallTo(() => _providerApi.SendProviderPayRequest(A<ProviderPayApiRequest>._)).Returns(apiResponse);
        A.CallTo(() => _mediator.Send(A<GetProviderPayByFobtId>._,CancellationToken.None)).Returns(default(ProviderPay));
        A.CallTo(() => _payableRules.IsPayable(A<PayableRuleAnswers>._))
            .Returns(new BusinessRuleStatus{IsMet = true});

        await _handler.Handle(A.Dummy<ProviderPayRequest>(), _messageSession);

        A.CallTo(() => _providerApi.SendProviderPayRequest(A<ProviderPayApiRequest>._))
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
        var exam = A.Fake<Core.Data.Entities.FOBT>();
        exam.EvaluationId = 1;
        A.CallTo(() => _mediator.Send(A<GetFOBT>._, A<CancellationToken>._)).Returns(exam);
        A.CallTo(() => _mapper.Map<ProviderPayApiRequest>(A<ProviderPayRequest>._))
            .Returns(A.Dummy<ProviderPayApiRequest>());
        A.CallTo(() => _providerApi.SendProviderPayRequest(A<ProviderPayApiRequest>._)).Returns(apiResponse);
        A.CallTo(() => _mediator.Send(A<GetProviderPayByFobtId>._,CancellationToken.None)).Returns(default(ProviderPay));
        A.CallTo(() => _payableRules.IsPayable(A<PayableRuleAnswers>._))
            .Returns(new BusinessRuleStatus{IsMet = true});
        
        await Assert.ThrowsAsync<ProviderPayRequestException>(async ()=> await _handler.Handle(A.Dummy<ProviderPayRequest>(), _messageSession));

        A.CallTo(() => _providerApi.SendProviderPayRequest(A<ProviderPayApiRequest>._))
            .MustHaveHappenedOnceExactly();
        Assert.Empty(_messageSession.SentMessages);
    }

    [Fact]
    public async Task Handler_Raise_SaveProviderPayEvent_When_ProviderPayAbsentInDb_And_ApiSuccess303ButNoPaymentIdReturned()
    {
        var apiResponse = new ApiResponse<ProviderPayApiResponse>(new HttpResponseMessage(HttpStatusCode.SeeOther),
            A.Dummy<ProviderPayApiResponse>(), null!);
        var exam = A.Fake<Core.Data.Entities.FOBT>();
        exam.EvaluationId = 1;
        A.CallTo(() => _mediator.Send(A<GetFOBT>._, A<CancellationToken>._)).Returns(exam);
        A.CallTo(() => _mapper.Map<ProviderPayApiRequest>(A<ProviderPayRequest>._))
            .Returns(A.Dummy<ProviderPayApiRequest>());
        A.CallTo(() => _providerApi.SendProviderPayRequest(A<ProviderPayApiRequest>._)).Returns(apiResponse);
        A.CallTo(() => _mediator.Send(A<GetProviderPayByFobtId>._,CancellationToken.None)).Returns(default(ProviderPay));
        A.CallTo(() => _payableRules.IsPayable(A<PayableRuleAnswers>._))
            .Returns(new BusinessRuleStatus{IsMet = true});

        await Assert.ThrowsAsync<ProviderPayRequestException>(async () =>
            await _handler.Handle(A.Dummy<ProviderPayRequest>(), _messageSession));

        A.CallTo(() => _providerApi.SendProviderPayRequest(A<ProviderPayApiRequest>._))
            .MustHaveHappenedOnceExactly();
        Assert.Empty(_messageSession.SentMessages);
    } 

    [Fact]
    public async Task Handler_DoesNot_Raise_SaveProviderPayEvent_Nor_CallAPI_When_ProviderPayAlreadyPresentInDb()
    {
        var apiResponse = new ApiResponse<ProviderPayApiResponse>(new HttpResponseMessage(HttpStatusCode.SeeOther),
            A.Dummy<ProviderPayApiResponse>(), null!);
        var exam = A.Fake<Core.Data.Entities.FOBT>();
        exam.EvaluationId = 1;
        A.CallTo(() => _mediator.Send(A<GetFOBT>._, A<CancellationToken>._)).Returns(exam);
        A.CallTo(() => _mapper.Map<ProviderPayApiRequest>(A<ProviderPayRequest>._))
            .Returns(A.Dummy<ProviderPayApiRequest>());
        A.CallTo(() => _providerApi.SendProviderPayRequest(A<ProviderPayApiRequest>._)).Returns(apiResponse);
        A.CallTo(() => _mediator.Send(A<GetProviderPayByFobtId>._,CancellationToken.None)).Returns(A.Dummy<ProviderPay>());
        A.CallTo(() => _payableRules.IsPayable(A<PayableRuleAnswers>._))
            .Returns(new BusinessRuleStatus{IsMet = true});

        await _handler.Handle(A.Dummy<ProviderPayRequest>(), _messageSession);

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
        var exam = A.Fake<Core.Data.Entities.FOBT>();
        exam.EvaluationId = 1;
        A.CallTo(() => _mediator.Send(A<GetFOBT>._, A<CancellationToken>._)).Returns(exam);
        A.CallTo(() => _mapper.Map<ProviderPayApiRequest>(A<ProviderPayRequest>._))
            .Returns(A.Dummy<ProviderPayApiRequest>());
        A.CallTo(() => _providerApi.SendProviderPayRequest(A<ProviderPayApiRequest>._)).Returns(apiResponse);
        A.CallTo(() => _mediator.Send(A<GetProviderPayByFobtId>._,CancellationToken.None)).Returns(default(ProviderPay));
        A.CallTo(() => _payableRules.IsPayable(A<PayableRuleAnswers>._))
            .Returns(new BusinessRuleStatus{IsMet = true});
        
        await Assert.ThrowsAsync<ProviderPayRequestException>(async () =>
            await _handler.Handle(A.Dummy<ProviderPayRequest>(), _messageSession));

        A.CallTo(() => _providerApi.SendProviderPayRequest(A<ProviderPayApiRequest>._))
            .MustHaveHappenedOnceExactly();
        Assert.Empty(_messageSession.SentMessages);
    }
}
