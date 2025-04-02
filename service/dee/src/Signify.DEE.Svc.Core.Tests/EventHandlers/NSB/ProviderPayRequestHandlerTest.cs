using System;
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
using Signify.DEE.Svc.Core.ApiClient;
using Signify.DEE.Svc.Core.ApiClient.Requests;
using Signify.DEE.Svc.Core.ApiClient.Responses;
using Signify.DEE.Svc.Core.BusinessRules;
using Signify.DEE.Svc.Core.Commands;
using Signify.DEE.Svc.Core.Constants;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.EventHandlers.Nsb;
using Signify.DEE.Svc.Core.Events.Status;
using Signify.DEE.Svc.Core.Exceptions;
using Signify.DEE.Svc.Core.Infrastructure;
using Signify.DEE.Svc.Core.Messages.Models;
using Signify.DEE.Svc.Core.Messages.Queries;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.EventHandlers.Nsb;

public class ProviderPayRequestHandlerTest
{
    private readonly IMapper _mapper;
    private readonly ProviderPayRequestHandler _providerPayHandler;
    private readonly IProviderPayApi _providerPayApi;
    private readonly ILogger<ProviderPayRequestHandler> _logger;
    private readonly TestableInvokeHandlerContext _messageSession;
    private readonly IMediator _mediator;
    private readonly IPublishObservability _publishObservability;
    private readonly FakeTransactionSupplier _transactionSupplier = new();
    private readonly IApplicationTime _applicationTime = new FakeApplicationTime();
    private readonly IPayableRules _payableRules = A.Fake<IPayableRules>();

    public ProviderPayRequestHandlerTest()
    {
        _messageSession = new TestableInvokeHandlerContext();
        _logger = A.Fake<ILogger<ProviderPayRequestHandler>>();
        _mapper = A.Fake<IMapper>();
        _providerPayApi = A.Fake<IProviderPayApi>();
        _mediator = A.Fake<IMediator>();
        _publishObservability = A.Fake<IPublishObservability>();
        _providerPayHandler =
            new ProviderPayRequestHandler(_logger, _mediator, _mapper, _providerPayApi, _publishObservability, _transactionSupplier, _payableRules);
    }

    [Fact]
    public async Task Handler_When_PayRules_Are_Not_Met()
    {
        var message = A.Fake<ProviderPayRequest>();
        message.ParentEventDateTime = _applicationTime.UtcNow().AddDays(-1);
        message.ParentEventReceivedDateTime = _applicationTime.UtcNow();
        var payableStatus = A.Fake<BusinessRuleStatus>();
        payableStatus.IsMet = false;
        payableStatus.Reason = "Exam not gradable";
        A.CallTo(() => _mediator.Send(A<GetProviderPayId>._, A<CancellationToken>._)).Returns("12345");
        A.CallTo(() => _payableRules.IsPayable(A<PayableRuleAnswers>._)).Returns(payableStatus);

        await _providerPayHandler.Handle(message, _messageSession);

        A.CallTo(() => _mediator.Send(A<UpdateExamStatus>.That.Matches(a =>
            a.ExamStatus.StatusCode == ExamStatusCode.ProviderNonPayableEventReceived &&
            ((ProviderPayStatusEvent)a.ExamStatus).Reason == payableStatus.Reason), A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        _transactionSupplier.AssertCommit();
        A.CallTo(() => _publishObservability.Commit()).MustHaveHappenedOnceExactly();
        Assert.Empty(_messageSession.SentMessages);
    }


    [Fact]
    public async Task Handler_When_GetExamStatus_Throws_Exception()
    {
        var message = A.Fake<ProviderPayRequest>();
        message.ParentEventDateTime = _applicationTime.UtcNow().AddDays(-1);
        message.ParentEventReceivedDateTime = _applicationTime.UtcNow();
        A.CallTo(() => _mediator.Send(A<GetAllExamStatus>._, A<CancellationToken>._)).Throws<Exception>();

        await Assert.ThrowsAnyAsync<Exception>(async () => await _providerPayHandler.Handle(message, _messageSession));

        _transactionSupplier.AssertRollback();
        A.CallTo(() => _publishObservability.Commit()).MustNotHaveHappened();
        Assert.Empty(_messageSession.SentMessages);
    }

    [Fact]
    public async Task Handler_When_PayRules_Are_Met_And_ProviderPay_AlreadyExist()
    {
        var message = A.Fake<ProviderPayRequest>();
        message.ParentEventDateTime = _applicationTime.UtcNow().AddDays(-1);
        message.ParentEventReceivedDateTime = _applicationTime.UtcNow();
        var payableStatus = A.Fake<BusinessRuleStatus>();
        payableStatus.IsMet = true;
        A.CallTo(() => _mediator.Send(A<GetProviderPayId>._, A<CancellationToken>._)).Returns("12345");
        A.CallTo(() => _payableRules.IsPayable(A<PayableRuleAnswers>._)).Returns(payableStatus);

        await _providerPayHandler.Handle(message, _messageSession);

        A.CallTo(() => _mediator.Send(A<UpdateExamStatus>.That.Matches(a =>
            a.ExamStatus.StatusCode == ExamStatusCode.ProviderPayableEventReceived), A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        _transactionSupplier.AssertCommit();
        A.CallTo(() => _publishObservability.Commit()).MustHaveHappenedOnceExactly();
        Assert.Empty(_messageSession.SentMessages);
    }

    [Fact]
    public async Task Handler_When_PayRules_Are_Met_And_ProviderPay_Is_New_And_CenseoId_Is_Invalid()
    {
        var message = A.Fake<ProviderPayRequest>();
        message.ParentEventDateTime = _applicationTime.UtcNow().AddDays(-1);
        message.ParentEventReceivedDateTime = _applicationTime.UtcNow();
        var payableStatus = A.Fake<BusinessRuleStatus>();
        payableStatus.IsMet = true;
        A.CallTo(() => _payableRules.IsPayable(A<PayableRuleAnswers>._)).Returns(payableStatus);
        A.CallTo(() => _mediator.Send(A<GetProviderPayId>._, A<CancellationToken>._)).Returns(string.Empty);
        A.CallTo(() => _mediator.Send(A<GetMemberInfo>._, A<CancellationToken>._)).Returns((MemberInfoRs)null);

        await Assert.ThrowsAnyAsync<ProviderPayException>(async () => await _providerPayHandler.Handle(message, _messageSession));

        A.CallTo(() => _mediator.Send(A<UpdateExamStatus>.That.Matches(a =>
            a.ExamStatus.StatusCode == ExamStatusCode.ProviderPayableEventReceived), A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        _transactionSupplier.AssertRollback();
        A.CallTo(() => _publishObservability.Commit()).MustNotHaveHappened();
        Assert.Empty(_messageSession.SentMessages);
    }

    [Fact]
    public async Task Handler_When_PayRules_Are_Met_And_ProviderPay_Is_New_And_GetCenseoId_Throws()
    {
        var message = A.Fake<ProviderPayRequest>();
        message.ParentEventDateTime = _applicationTime.UtcNow().AddDays(-1);
        message.ParentEventReceivedDateTime = _applicationTime.UtcNow();
        var payableStatus = A.Fake<BusinessRuleStatus>();
        payableStatus.IsMet = true;
        A.CallTo(() => _payableRules.IsPayable(A<PayableRuleAnswers>._)).Returns(payableStatus);
        A.CallTo(() => _mediator.Send(A<GetProviderPayId>._, A<CancellationToken>._)).Returns(string.Empty);
        A.CallTo(() => _mediator.Send(A<GetMemberInfo>._, A<CancellationToken>._)).Throws<Exception>();

        await Assert.ThrowsAnyAsync<Exception>(async () => await _providerPayHandler.Handle(message, _messageSession));

        A.CallTo(() => _mediator.Send(A<UpdateExamStatus>.That.Matches(a =>
            a.ExamStatus.StatusCode == ExamStatusCode.ProviderPayableEventReceived), A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        _transactionSupplier.AssertRollback();
        A.CallTo(() => _publishObservability.Commit()).MustNotHaveHappened();
        Assert.Empty(_messageSession.SentMessages);
        A.CallTo(() => _mediator.Send(A<GetMemberInfo>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Handler_When_PayRules_Are_Met_And_ProviderPay_Is_New_And_CenseoId_Is_Valid_And_ProviderPayAPI_SeeOther()
    {
        var message = A.Fake<ProviderPayRequest>();
        message.ParentEventDateTime = _applicationTime.UtcNow().AddDays(-1);
        message.ParentEventReceivedDateTime = _applicationTime.UtcNow();
        message.ParentEvent = "CDIPassedEvent";
        var memberInfo = A.Fake<MemberInfoRs>();
        memberInfo.CenseoId = "X1234";
        var providerPayRequest = A.Fake<ProviderPayApiRequest>();
        providerPayRequest.PersonId = memberInfo.CenseoId;
        var apiResponseBody = A.Fake<ProviderPayApiResponse>();
        apiResponseBody.PaymentId = Guid.NewGuid().ToString();
        var apiResponse = new ApiResponse<ProviderPayApiResponse>(new HttpResponseMessage(HttpStatusCode.SeeOther),
            apiResponseBody, null!);
        apiResponse.Headers.Location = new Uri("http://blah/12345");
        var payableStatus = A.Fake<BusinessRuleStatus>();
        payableStatus.IsMet = true;
        A.CallTo(() => _payableRules.IsPayable(A<PayableRuleAnswers>._)).Returns(payableStatus);
        A.CallTo(() => _mediator.Send(A<GetProviderPayId>._, A<CancellationToken>._)).Returns(string.Empty);
        A.CallTo(() => _mediator.Send(A<GetMemberInfo>._, A<CancellationToken>._)).Returns(memberInfo);
        A.CallTo(() => _mapper.Map<ProviderPayApiRequest>(A<ProviderPayRequest>._)).Returns(providerPayRequest);
        A.CallTo(() => _providerPayApi.SendProviderPayRequest(A<ProviderPayApiRequest>._)).Returns(apiResponse);
        var mappedSaveProviderPay = A.Fake<SaveProviderPay>();
        A.CallTo(() => _mapper.Map<SaveProviderPay>(A<ProviderPayRequest>._)).Returns(mappedSaveProviderPay);
        mappedSaveProviderPay.ParentEventDateTime = message.ParentEventDateTime;
        mappedSaveProviderPay.ParentEventReceivedDateTime = message.ParentEventReceivedDateTime;
        mappedSaveProviderPay.ParentEvent = message.ParentEvent;

        await _providerPayHandler.Handle(message, _messageSession);

        A.CallTo(() => _mediator.Send(A<UpdateExamStatus>.That.Matches(a =>
            a.ExamStatus.StatusCode == ExamStatusCode.ProviderPayableEventReceived), A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _publishObservability.RegisterEvent(
                A<ObservabilityEvent>.That.Matches(a => a.EventType == Observability.ProviderPay.ProviderPayApiStatusCodeEvent), true))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _publishObservability.RegisterEvent(
                A<ObservabilityEvent>.That.Matches(a => a.EventType == Observability.ProviderPay.ProviderPayOrBillingEvent), false))
            .MustHaveHappenedOnceExactly();
        Assert.Single(_messageSession.SentMessages);
        var saveRequest = _messageSession.FindSentMessage<SaveProviderPay>();
        Assert.NotNull(saveRequest);
        Assert.Equal(message.ParentEventReceivedDateTime, saveRequest.ParentEventReceivedDateTime);
        Assert.Equal(message.ParentEventDateTime, saveRequest.ParentEventDateTime);
        Assert.Equal(message.ParentEvent, saveRequest.ParentEvent);
        _transactionSupplier.AssertCommit();
        A.CallTo(() => _publishObservability.Commit()).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mapper.Map<ProviderPayApiRequest>(A<ProviderPayRequest>._)).MustHaveHappenedOnceExactly();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Handler_When_PayRules_Met_And_ProviderPay_IsNew_And_CenseoId_IsValid_And_ProviderPayAPI_Success_But_empty(string paymentId)
    {
        var message = A.Fake<ProviderPayRequest>();
        message.ParentEventDateTime = _applicationTime.UtcNow().AddDays(-1);
        message.ParentEventReceivedDateTime = _applicationTime.UtcNow();
        message.ParentEvent = "CDIPassedEvent";
        var memberInfo = A.Fake<MemberInfoRs>();
        memberInfo.CenseoId = "X1234";
        var providerPayRequest = A.Fake<ProviderPayApiRequest>();
        providerPayRequest.PersonId = memberInfo.CenseoId;
        var apiResponseBody = A.Fake<ProviderPayApiResponse>();
        apiResponseBody.PaymentId = paymentId;
        var apiResponse = new ApiResponse<ProviderPayApiResponse>(new HttpResponseMessage(HttpStatusCode.Accepted),
            apiResponseBody, null!);
        var payableStatus = A.Fake<BusinessRuleStatus>();
        payableStatus.IsMet = true;
        A.CallTo(() => _payableRules.IsPayable(A<PayableRuleAnswers>._)).Returns(payableStatus);
        A.CallTo(() => _mediator.Send(A<GetProviderPayId>._, A<CancellationToken>._)).Returns(string.Empty);
        A.CallTo(() => _mediator.Send(A<GetMemberInfo>._, A<CancellationToken>._)).Returns(memberInfo);
        A.CallTo(() => _mapper.Map<ProviderPayApiRequest>(A<ProviderPayRequest>._)).Returns(providerPayRequest);
        A.CallTo(() => _providerPayApi.SendProviderPayRequest(A<ProviderPayApiRequest>._)).Returns(apiResponse);

        await Assert.ThrowsAsync<ProviderPayException>(async () => await _providerPayHandler.Handle(message, _messageSession));

        A.CallTo(() => _mediator.Send(A<UpdateExamStatus>.That.Matches(a =>
            a.ExamStatus.StatusCode == ExamStatusCode.ProviderPayableEventReceived), A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _publishObservability.RegisterEvent(
                A<ObservabilityEvent>.That.Matches(a => a.EventType == Observability.ProviderPay.ProviderPayApiStatusCodeEvent), true))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _publishObservability.RegisterEvent(
                A<ObservabilityEvent>.That.Matches(a => a.EventType == Observability.ProviderPay.ProviderPayOrBillingEvent), false))
            .MustHaveHappenedOnceExactly();
        _transactionSupplier.AssertRollback();
        A.CallTo(() => _publishObservability.Commit()).MustNotHaveHappened();
        A.CallTo(() => _mapper.Map<ProviderPayApiRequest>(A<ProviderPayRequest>._)).MustHaveHappenedOnceExactly();
        Assert.Empty(_messageSession.SentMessages);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task Handler_When_PayRules_Met_And_ProviderPay_IsNew_And_CenseoId_IsValid_And_ProviderPayAPI_Fails(HttpStatusCode statusCode)
    {
        var message = A.Fake<ProviderPayRequest>();
        message.ParentEventDateTime = _applicationTime.UtcNow().AddDays(-1);
        message.ParentEventReceivedDateTime = _applicationTime.UtcNow();
        message.ParentEvent = "CDIPassedEvent";
        var memberInfo = A.Fake<MemberInfoRs>();
        memberInfo.CenseoId = "X1234";
        var providerPayRequest = A.Fake<ProviderPayApiRequest>();
        providerPayRequest.PersonId = memberInfo.CenseoId;
        var apiResponseBody = A.Fake<ProviderPayApiResponse>();
        var apiResponse = new ApiResponse<ProviderPayApiResponse>(new HttpResponseMessage(statusCode),
            apiResponseBody, null!);
        var payableStatus = A.Fake<BusinessRuleStatus>();
        payableStatus.IsMet = true;
        A.CallTo(() => _payableRules.IsPayable(A<PayableRuleAnswers>._)).Returns(payableStatus);
        A.CallTo(() => _mediator.Send(A<GetProviderPayId>._, A<CancellationToken>._)).Returns(string.Empty);
        A.CallTo(() => _mediator.Send(A<GetMemberInfo>._, A<CancellationToken>._)).Returns(memberInfo);
        A.CallTo(() => _mapper.Map<ProviderPayApiRequest>(A<ProviderPayRequest>._)).Returns(providerPayRequest);
        A.CallTo(() => _providerPayApi.SendProviderPayRequest(A<ProviderPayApiRequest>._)).Returns(apiResponse);

        await Assert.ThrowsAsync<ProviderPayException>(async () => await _providerPayHandler.Handle(message, _messageSession));

        A.CallTo(() => _mediator.Send(A<UpdateExamStatus>.That.Matches(a =>
            a.ExamStatus.StatusCode == ExamStatusCode.ProviderPayableEventReceived), A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _publishObservability.RegisterEvent(
                A<ObservabilityEvent>.That.Matches(a => a.EventType == Observability.ProviderPay.ProviderPayApiStatusCodeEvent), true))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _publishObservability.RegisterEvent(
                A<ObservabilityEvent>.That.Matches(a => a.EventType == Observability.ProviderPay.ProviderPayOrBillingEvent), false))
            .MustHaveHappenedOnceExactly();
        _transactionSupplier.AssertRollback();
        A.CallTo(() => _publishObservability.Commit()).MustNotHaveHappened();
        A.CallTo(() => _mapper.Map<ProviderPayApiRequest>(A<ProviderPayRequest>._)).MustHaveHappenedOnceExactly();
        Assert.Empty(_messageSession.SentMessages);
    }

    [Fact]
    public async Task Handler_When_PayRules_Are_Met_And_ProviderPay_Is_New_And_CenseoId_Is_Valid_And_ProviderPayAPI_Success()
    {
        var message = A.Fake<ProviderPayRequest>();
        message.ParentEventDateTime = _applicationTime.UtcNow().AddDays(-1);
        message.ParentEventReceivedDateTime = _applicationTime.UtcNow();
        message.ParentEvent = "CDIPassedEvent";
        var memberInfo = A.Fake<MemberInfoRs>();
        memberInfo.CenseoId = "X1234";
        var providerPayRequest = A.Fake<ProviderPayApiRequest>();
        providerPayRequest.PersonId = memberInfo.CenseoId;
        var apiResponseBody = A.Fake<ProviderPayApiResponse>();
        apiResponseBody.PaymentId = Guid.NewGuid().ToString();
        var apiResponse = new ApiResponse<ProviderPayApiResponse>(new HttpResponseMessage(HttpStatusCode.Accepted),
            apiResponseBody, null!);
        var payableStatus = A.Fake<BusinessRuleStatus>();
        payableStatus.IsMet = true;
        A.CallTo(() => _payableRules.IsPayable(A<PayableRuleAnswers>._)).Returns(payableStatus);
        A.CallTo(() => _mediator.Send(A<GetProviderPayId>._, A<CancellationToken>._)).Returns(string.Empty);
        A.CallTo(() => _mediator.Send(A<GetMemberInfo>._, A<CancellationToken>._)).Returns(memberInfo);
        A.CallTo(() => _mapper.Map<ProviderPayApiRequest>(A<ProviderPayRequest>._)).Returns(providerPayRequest);
        A.CallTo(() => _providerPayApi.SendProviderPayRequest(A<ProviderPayApiRequest>._)).Returns(apiResponse);
        var mappedSaveProviderPay = A.Fake<SaveProviderPay>();
        A.CallTo(() => _mapper.Map<SaveProviderPay>(A<ProviderPayRequest>._)).Returns(mappedSaveProviderPay);
        mappedSaveProviderPay.ParentEventDateTime = message.ParentEventDateTime;
        mappedSaveProviderPay.ParentEventReceivedDateTime = message.ParentEventReceivedDateTime;
        mappedSaveProviderPay.ParentEvent = message.ParentEvent;

        await _providerPayHandler.Handle(message, _messageSession);

        A.CallTo(() => _mediator.Send(A<UpdateExamStatus>.That.Matches(a =>
            a.ExamStatus.StatusCode == ExamStatusCode.ProviderPayableEventReceived), A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _publishObservability.RegisterEvent(
                A<ObservabilityEvent>.That.Matches(a => a.EventType == Observability.ProviderPay.ProviderPayApiStatusCodeEvent), true))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _publishObservability.RegisterEvent(
                A<ObservabilityEvent>.That.Matches(a => a.EventType == Observability.ProviderPay.ProviderPayOrBillingEvent), false))
            .MustHaveHappenedOnceExactly();
        Assert.Single(_messageSession.SentMessages);
        var saveRequest = _messageSession.FindSentMessage<SaveProviderPay>();
        Assert.NotNull(saveRequest);
        Assert.Equal(message.ParentEventReceivedDateTime, saveRequest.ParentEventReceivedDateTime);
        Assert.Equal(message.ParentEventDateTime, saveRequest.ParentEventDateTime);
        Assert.Equal(message.ParentEvent, saveRequest.ParentEvent);
        _transactionSupplier.AssertCommit();
        A.CallTo(() => _publishObservability.Commit()).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mapper.Map<ProviderPayApiRequest>(A<ProviderPayRequest>._)).MustHaveHappenedOnceExactly();
    }
}