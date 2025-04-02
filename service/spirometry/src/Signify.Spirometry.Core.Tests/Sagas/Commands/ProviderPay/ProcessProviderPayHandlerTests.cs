using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Refit;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using Signify.Spirometry.Core.ApiClients.ProviderPayApi.Requests;
using Signify.Spirometry.Core.ApiClients.ProviderPayApi.Responses;
using Signify.Spirometry.Core.ApiClients.ProviderPayApi;
using Signify.Spirometry.Core.Commands;
using Signify.Spirometry.Core.Constants;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Events;
using Signify.Spirometry.Core.Exceptions;
using Signify.Spirometry.Core.Queries;
using SpiroNsb.SagaCommands;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;
using StatusCode = Signify.Spirometry.Core.Models.StatusCode;

namespace Signify.Spirometry.Core.Tests.Sagas.Commands.ProviderPay;

public class ProcessProviderPayHandlerTests
{
    private readonly FakeTransactionSupplier _transactionSupplier = new();
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly IProviderPayApi _providerPayApi = A.Fake<IProviderPayApi>();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();

    private ProcessProviderPayHandler CreateSubject()
        => new(A.Dummy<ILogger<ProcessProviderPayHandler>>(), _transactionSupplier, _publishObservability, _mediator, _mapper, _providerPayApi);

    [Fact]
    public async Task Handle_When_ExamNotFound_Throws_Exception()
    {
        var request = A.Fake<ProcessProviderPay>();
        A.CallTo(() => _mediator.Send(A<QuerySpirometryExam>._, A<CancellationToken>._)).Returns((SpirometryExam)null);

        await Assert.ThrowsAnyAsync<ExamNotFoundException>(async () => await CreateSubject().Handle(request, new TestableMessageHandlerContext()));

        _transactionSupplier.AssertRollback();
    }

    [Fact]
    public async Task Handle_When_NoUnprocessedCdiEvents_Ends_Without_AddToDb()
    {
        var request = A.Fake<ProcessProviderPay>();
        var exam = CreateExamWithStatus(Core.Data.Entities.StatusCode.SpirometryExamPerformed);
        var unprocessedEvents = new List<CdiEventForPayment>();
        A.CallTo(() => _mediator.Send(A<QuerySpirometryExam>._, A<CancellationToken>._)).Returns(exam);
        A.CallTo(() => _mediator.Send(A<QueryUnprocessedCdiEventForPayments>._, A<CancellationToken>._)).Returns(unprocessedEvents);

        await CreateSubject().Handle(request, new TestableMessageHandlerContext());

        _transactionSupplier.AssertCommit();
        A.CallTo(() => _publishObservability.Commit()).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<ExamStatusEvent>._, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task Handle_When_UnprocessedCdiEvents_Are_Present_Updates_Db()
    {
        var request = A.Fake<ProcessProviderPay>();
        var exam = CreateExamWithStatus(Core.Data.Entities.StatusCode.SpirometryExamPerformed);
        var unprocessedEvents = A.Fake<List<CdiEventForPayment>>();
        unprocessedEvents.Add(new CdiEventForPayment
        {
            EvaluationId = 123,
            EventType = nameof(CDIFailedEvent),
            PayProvider = false
        });
        unprocessedEvents.Add(new CdiEventForPayment
        {
            EvaluationId = 123,
            EventType = nameof(CDIPassedEvent)
        });
        A.CallTo(() => _mediator.Send(A<QuerySpirometryExam>._, A<CancellationToken>._)).Returns(exam);
        A.CallTo(() => _mediator.Send(A<QueryUnprocessedCdiEventForPayments>._, A<CancellationToken>._)).Returns(unprocessedEvents);

        await CreateSubject().Handle(request, new TestableMessageHandlerContext());

        _transactionSupplier.AssertCommit();
        A.CallTo(() => _publishObservability.Commit()).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(
            A<ExamStatusEvent>.That.Matches(s => s.StatusCode == StatusCode.CdiPassedReceived || s.StatusCode == StatusCode.CdiFailedWithoutPayReceived),
            A<CancellationToken>._)).MustHaveHappened(2, Times.Exactly);
    }

    [Theory]
    [InlineData(nameof(CDIFailedEvent), false)]
    public async Task Handle_When_EventType_Is_CdiFailedWithoutPay_NonPayableEvent_IsRaised(string eventType, bool payProvider)
    {
        var request = A.Fake<ProcessProviderPay>();
        var exam = CreateExamWithStatus(Core.Data.Entities.StatusCode.SpirometryExamPerformed);
        var unprocessedEvents = A.Fake<List<CdiEventForPayment>>();
        unprocessedEvents.Add(new CdiEventForPayment
        {
            EvaluationId = 123,
            EventType = eventType,
            PayProvider = payProvider
        });
        A.CallTo(() => _mediator.Send(A<QuerySpirometryExam>._, A<CancellationToken>._)).Returns(exam);
        A.CallTo(() => _mediator.Send(A<QueryUnprocessedCdiEventForPayments>._, A<CancellationToken>._)).Returns(unprocessedEvents);

        await CreateSubject().Handle(request, new TestableMessageHandlerContext());

        _transactionSupplier.AssertCommit();
        A.CallTo(() => _publishObservability.Commit()).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<ExamStatusEvent>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(
                A<ExamStatusEvent>.That.Matches(s =>
                    s.StatusCode == StatusCode.ProviderNonPayableEventReceived && s.Reason == "PayProvider is false for the CDIFailedEvent"),
                A<CancellationToken>._))
            .MustHaveHappened();
    }

    [Theory]
    [InlineData(nameof(CDIPassedEvent), true)]
    [InlineData(nameof(CDIFailedEvent), true)]
    public async Task Handle_When_EventType_Is_PayableType_But_NonPayable_Then_NonPayableEvent_IsRaised(string eventType, bool payProvider)
    {
        var request = A.Fake<ProcessProviderPay>();
        var exam = CreateExamWithStatus(Core.Data.Entities.StatusCode.SpirometryExamPerformed);
        var unprocessedEvents = A.Fake<List<CdiEventForPayment>>();
        unprocessedEvents.Add(new CdiEventForPayment
        {
            EvaluationId = 123,
            EventType = eventType,
            PayProvider = payProvider
        });
        var payableResult = A.Fake<QueryPayableResult>();
        A.CallTo(() => _mediator.Send(A<QuerySpirometryExam>._, A<CancellationToken>._)).Returns(exam);
        A.CallTo(() => _mediator.Send(A<QueryUnprocessedCdiEventForPayments>._, A<CancellationToken>._)).Returns(unprocessedEvents);
        A.CallTo(() => _mediator.Send(A<QueryPayable>._, A<CancellationToken>._)).Returns(payableResult);

        await CreateSubject().Handle(request, new TestableMessageHandlerContext());

        _transactionSupplier.AssertCommit();
        A.CallTo(() => _publishObservability.Commit()).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<ExamStatusEvent>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(
                A<ExamStatusEvent>.That.Matches(
                    s => s.StatusCode == StatusCode.ProviderNonPayableEventReceived && s.Reason == "Payment rules are not satisfied"), A<CancellationToken>._))
            .MustHaveHappened();
    }

    [Theory]
    [InlineData(nameof(CDIPassedEvent), true)]
    [InlineData(nameof(CDIFailedEvent), true)]
    public async Task Handle_When_EventType_Is_PayableType_And_IsPayable_Then_PayableEvent_IsRaised(string eventType, bool payProvider)
    {
        var request = A.Fake<ProcessProviderPay>();
        var exam = CreateExamWithStatus(Core.Data.Entities.StatusCode.SpirometryExamPerformed);
        var unprocessedEvents = A.Fake<List<CdiEventForPayment>>();
        unprocessedEvents.Add(new CdiEventForPayment
        {
            EvaluationId = 123,
            EventType = eventType,
            PayProvider = payProvider
        });
        var payableResult = A.Fake<QueryPayableResult>(o => o.WithArgumentsForConstructor(new object[] { true }));
        A.CallTo(() => _mediator.Send(A<QuerySpirometryExam>._, A<CancellationToken>._)).Returns(exam);
        A.CallTo(() => _mediator.Send(A<QueryUnprocessedCdiEventForPayments>._, A<CancellationToken>._)).Returns(unprocessedEvents);
        A.CallTo(() => _mediator.Send(A<QueryPayable>._, A<CancellationToken>._)).Returns(payableResult);

        await CreateSubject().Handle(request, new TestableMessageHandlerContext());

        _transactionSupplier.AssertCommit();
        A.CallTo(() => _publishObservability.Commit()).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<ExamStatusEvent>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(
                A<ExamStatusEvent>.That.Matches(
                    s => s.StatusCode == StatusCode.ProviderPayableEventReceived), A<CancellationToken>._))
            .MustHaveHappened();
    }

    [Theory]
    [InlineData(nameof(CDIPassedEvent), true)]
    [InlineData(nameof(CDIFailedEvent), true)]
    public async Task Handle_When_Payable_But_AlreadyPaid_DoesNot_Invoke_ProviderPayApi(string eventType, bool payProvider)
    {
        var request = A.Fake<ProcessProviderPay>();
        var exam = CreateExamWithStatus(Core.Data.Entities.StatusCode.SpirometryExamPerformed);
        var unprocessedEvents = A.Fake<List<CdiEventForPayment>>();
        unprocessedEvents.Add(new CdiEventForPayment
        {
            EvaluationId = 123,
            EventType = eventType,
            PayProvider = payProvider
        });
        var payableResult = A.Fake<QueryPayableResult>(o => o.WithArgumentsForConstructor(new object[] { true }));
        A.CallTo(() => _mediator.Send(A<QuerySpirometryExam>._, A<CancellationToken>._)).Returns(exam);
        A.CallTo(() => _mediator.Send(A<QueryUnprocessedCdiEventForPayments>._, A<CancellationToken>._)).Returns(unprocessedEvents);
        A.CallTo(() => _mediator.Send(A<QueryPayable>._, A<CancellationToken>._)).Returns(payableResult);
        A.CallTo(() => _mediator.Send(A<QueryProviderPay>._, A<CancellationToken>._)).Returns(A.Dummy<Core.Data.Entities.ProviderPay>());

        await CreateSubject().Handle(request, new TestableMessageHandlerContext());

        _transactionSupplier.AssertCommit();
        A.CallTo(() => _publishObservability.Commit()).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<ExamStatusEvent>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(
                A<ExamStatusEvent>.That.Matches(
                    s => s.StatusCode == StatusCode.ProviderPayableEventReceived), A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => _mapper.Map<ProviderPayApiRequest>(A<SpirometryExam>._)).MustNotHaveHappened();
        A.CallTo(() => _providerPayApi.SendProviderPayRequest(A<ProviderPayApiRequest>._)).MustNotHaveHappened();
    }

    [Theory]
    [InlineData(nameof(CDIPassedEvent), true)]
    [InlineData(nameof(CDIFailedEvent), true)]
    public async Task Handle_When_Payable_And_NotAlreadyPaid_Invokes_ProviderPayApi(string eventType, bool payProvider)
    {
        var exam = CreateExamWithStatus(Core.Data.Entities.StatusCode.SpirometryExamPerformed);
        var apiResponseBody = A.Fake<ProviderPayApiResponse>();
        apiResponseBody.PaymentId = Guid.NewGuid().ToString();
        var apiResponse = new ApiResponse<ProviderPayApiResponse>(new HttpResponseMessage(HttpStatusCode.Accepted),
            apiResponseBody, null!);
        var request = A.Fake<ProcessProviderPay>();
        var unprocessedEvents = A.Fake<List<CdiEventForPayment>>();
        unprocessedEvents.Add(new CdiEventForPayment
        {
            EvaluationId = 123,
            EventType = eventType,
            PayProvider = payProvider
        });
        var payableResult = A.Fake<QueryPayableResult>(o => o.WithArgumentsForConstructor(new object[] { true }));
        A.CallTo(() => _mediator.Send(A<QuerySpirometryExam>._, A<CancellationToken>._)).Returns(exam);
        A.CallTo(() => _mediator.Send(A<QueryUnprocessedCdiEventForPayments>._, A<CancellationToken>._)).Returns(unprocessedEvents);
        A.CallTo(() => _mediator.Send(A<QueryPayable>._, A<CancellationToken>._)).Returns(payableResult);
        A.CallTo(() => _mediator.Send(A<QueryProviderPay>._, A<CancellationToken>._)).Returns((Core.Data.Entities.ProviderPay)null);
        A.CallTo(() => _providerPayApi.SendProviderPayRequest(A<ProviderPayApiRequest>._)).Returns(apiResponse);

        await CreateSubject().Handle(request, new TestableMessageHandlerContext());

        _transactionSupplier.AssertCommit();
        A.CallTo(() => _publishObservability.Commit()).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<ExamStatusEvent>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(
                A<ExamStatusEvent>.That.Matches(
                    s => s.StatusCode == StatusCode.ProviderPayableEventReceived), A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => _mapper.Map<ProviderPayApiRequest>(A<SpirometryExam>._)).MustHaveHappened();
        A.CallTo(() => _providerPayApi.SendProviderPayRequest(A<ProviderPayApiRequest>.That.Matches(p => p.AdditionalDetails.Count == 2))).MustHaveHappened();
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task Handle_When_Payable_And_NotAlreadyPaid_Invokes_ProviderPayApi_But_Api_Fails(HttpStatusCode statusCode)
    {
        var exam = CreateExamWithStatus(Core.Data.Entities.StatusCode.SpirometryExamPerformed);
        var apiResponseBody = A.Fake<ProviderPayApiResponse>();
        apiResponseBody.PaymentId = Guid.NewGuid().ToString();
        var apiResponse = new ApiResponse<ProviderPayApiResponse>(new HttpResponseMessage(statusCode),
            apiResponseBody, null!);
        var request = A.Fake<ProcessProviderPay>();
        var unprocessedEvents = A.Fake<List<CdiEventForPayment>>();
        unprocessedEvents.Add(new CdiEventForPayment
        {
            EvaluationId = 123,
            EventType = nameof(CDIFailedEvent),
            PayProvider = true
        });
        var payableResult = A.Fake<QueryPayableResult>(o => o.WithArgumentsForConstructor(new object[] { true }));
        A.CallTo(() => _mediator.Send(A<QuerySpirometryExam>._, A<CancellationToken>._)).Returns(exam);
        A.CallTo(() => _mediator.Send(A<QueryUnprocessedCdiEventForPayments>._, A<CancellationToken>._)).Returns(unprocessedEvents);
        A.CallTo(() => _mediator.Send(A<QueryPayable>._, A<CancellationToken>._)).Returns(payableResult);
        A.CallTo(() => _mediator.Send(A<QueryProviderPay>._, A<CancellationToken>._)).Returns((Core.Data.Entities.ProviderPay)null);
        A.CallTo(() => _providerPayApi.SendProviderPayRequest(A<ProviderPayApiRequest>._)).Returns(apiResponse);

        await Assert.ThrowsAnyAsync<ProviderPayRequestException>(async () => await CreateSubject().Handle(request, new TestableMessageHandlerContext()));

        _transactionSupplier.AssertRollback();
        A.CallTo(() => _publishObservability.Commit()).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<ExamStatusEvent>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(
                A<ExamStatusEvent>.That.Matches(
                    s => s.StatusCode == StatusCode.ProviderPayableEventReceived), A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => _mapper.Map<ProviderPayApiRequest>(A<SpirometryExam>._)).MustHaveHappened();
        A.CallTo(() => _providerPayApi.SendProviderPayRequest(A<ProviderPayApiRequest>._)).MustHaveHappened();
    }

    [Theory]
    [InlineData(nameof(CDIPassedEvent), true)]
    [InlineData(nameof(CDIFailedEvent), true)]
    public async Task Handle_When_Payable_And_NotAlreadyPaid_Invokes_ProviderPayApi_And_RegisterObservability(string eventType, bool payProvider)
    {
        var exam = CreateExamWithStatus(Core.Data.Entities.StatusCode.SpirometryExamPerformed);
        var apiResponseBody = A.Fake<ProviderPayApiResponse>();
        apiResponseBody.PaymentId = Guid.NewGuid().ToString();
        var apiResponse = new ApiResponse<ProviderPayApiResponse>(new HttpResponseMessage(HttpStatusCode.Accepted),
            apiResponseBody, null!);
        var request = A.Fake<ProcessProviderPay>();
        var unprocessedEvents = A.Fake<List<CdiEventForPayment>>();
        unprocessedEvents.Add(new CdiEventForPayment
        {
            EvaluationId = 123,
            EventType = eventType,
            PayProvider = payProvider
        });
        var payableResult = A.Fake<QueryPayableResult>(o => o.WithArgumentsForConstructor(new object[] { true }));
        A.CallTo(() => _mediator.Send(A<QuerySpirometryExam>._, A<CancellationToken>._)).Returns(exam);
        A.CallTo(() => _mediator.Send(A<QueryUnprocessedCdiEventForPayments>._, A<CancellationToken>._)).Returns(unprocessedEvents);
        A.CallTo(() => _mediator.Send(A<QueryPayable>._, A<CancellationToken>._)).Returns(payableResult);
        A.CallTo(() => _mediator.Send(A<QueryProviderPay>._, A<CancellationToken>._)).Returns((Core.Data.Entities.ProviderPay)null);
        A.CallTo(() => _providerPayApi.SendProviderPayRequest(A<ProviderPayApiRequest>._)).Returns(apiResponse);

        await CreateSubject().Handle(request, new TestableMessageHandlerContext());

        _transactionSupplier.AssertCommit();
        A.CallTo(() => _publishObservability.Commit()).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<ExamStatusEvent>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(
                A<ExamStatusEvent>.That.Matches(
                    s => s.StatusCode == StatusCode.ProviderPayableEventReceived), A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => _mapper.Map<ProviderPayApiRequest>(A<SpirometryExam>._)).MustHaveHappened();
        A.CallTo(() => _providerPayApi.SendProviderPayRequest(A<ProviderPayApiRequest>._)).MustHaveHappened();
        A.CallTo(() => _publishObservability.RegisterEvent(
            A<ObservabilityEvent>.That.Matches(o => o.EventType == Observability.ProviderPay.ProviderPayApiStatusCodeEvent), true)).MustHaveHappened();
        A.CallTo(() => _publishObservability.RegisterEvent(
            A<ObservabilityEvent>.That.Matches(o => o.EventType == Observability.ProviderPay.ProviderPayOrBillingEvent), false)).MustHaveHappened();
    }

    [Theory]
    [InlineData(nameof(CDIPassedEvent), true)]
    [InlineData(nameof(CDIFailedEvent), true)]
    public async Task Handle_When_Payable_Invokes_ProviderPayApi_And_PublishEvent_EvenIf_RegisterObservability_ThrowsException(
        string eventType, bool payProvider)
    {
        var exam = CreateExamWithStatus(Core.Data.Entities.StatusCode.SpirometryExamPerformed);
        var apiResponseBody = A.Fake<ProviderPayApiResponse>();
        apiResponseBody.PaymentId = Guid.NewGuid().ToString();
        var apiResponse = new ApiResponse<ProviderPayApiResponse>(new HttpResponseMessage(HttpStatusCode.Accepted),
            apiResponseBody, null!);
        var request = A.Fake<ProcessProviderPay>();
        var unprocessedEvents = A.Fake<List<CdiEventForPayment>>();
        unprocessedEvents.Add(new CdiEventForPayment
        {
            EvaluationId = 123,
            EventType = eventType,
            PayProvider = payProvider
        });
        var payableResult = A.Fake<QueryPayableResult>(o => o.WithArgumentsForConstructor(new object[] { true }));
        A.CallTo(() => _mediator.Send(A<QuerySpirometryExam>._, A<CancellationToken>._)).Returns(exam);
        A.CallTo(() => _mediator.Send(A<QueryUnprocessedCdiEventForPayments>._, A<CancellationToken>._)).Returns(unprocessedEvents);
        A.CallTo(() => _mediator.Send(A<QueryPayable>._, A<CancellationToken>._)).Returns(payableResult);
        A.CallTo(() => _mediator.Send(A<QueryProviderPay>._, A<CancellationToken>._)).Returns((Core.Data.Entities.ProviderPay)null);
        A.CallTo(() => _providerPayApi.SendProviderPayRequest(A<ProviderPayApiRequest>._)).Returns(apiResponse);
        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>._, A<bool>._)).Throws<Exception>();

        await CreateSubject().Handle(request, new TestableMessageHandlerContext());

        _transactionSupplier.AssertCommit();
        A.CallTo(() => _publishObservability.Commit()).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<ExamStatusEvent>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(
                A<ExamStatusEvent>.That.Matches(
                    s => s.StatusCode == StatusCode.ProviderPayableEventReceived), A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => _mapper.Map<ProviderPayApiRequest>(A<SpirometryExam>._)).MustHaveHappened();
        A.CallTo(() => _providerPayApi.SendProviderPayRequest(A<ProviderPayApiRequest>._)).MustHaveHappened();
        A.CallTo(() => _publishObservability.RegisterEvent(
            A<ObservabilityEvent>.That.Matches(o => o.EventType == Observability.ProviderPay.ProviderPayApiStatusCodeEvent), true)).MustHaveHappened();
        A.CallTo(() => _publishObservability.RegisterEvent(
            A<ObservabilityEvent>.That.Matches(o => o.EventType == Observability.ProviderPay.ProviderPayOrBillingEvent), false)).MustHaveHappened();
    }

    [Fact]
    public async Task Handle_When_ProviderPayApi_Returns_202_GetsPaymentId_And_Invokes_SaveProviderPay()
    {
        var exam = CreateExamWithStatus(Core.Data.Entities.StatusCode.SpirometryExamPerformed);
        var paymentId = Guid.NewGuid().ToString();
        var apiResponseBody = A.Fake<ProviderPayApiResponse>();
        apiResponseBody.PaymentId = paymentId;
        var apiResponse = new ApiResponse<ProviderPayApiResponse>(new HttpResponseMessage(HttpStatusCode.Accepted),
            apiResponseBody, null!);
        var request = A.Fake<ProcessProviderPay>();
        var unprocessedEvents = A.Fake<List<CdiEventForPayment>>();
        unprocessedEvents.Add(new CdiEventForPayment
        {
            EvaluationId = 123,
            EventType = nameof(CDIPassedEvent)
        });
        var payableResult = A.Fake<QueryPayableResult>(o => o.WithArgumentsForConstructor(new object[] { true }));
        A.CallTo(() => _mediator.Send(A<QuerySpirometryExam>._, A<CancellationToken>._)).Returns(exam);
        A.CallTo(() => _mediator.Send(A<QueryUnprocessedCdiEventForPayments>._, A<CancellationToken>._)).Returns(unprocessedEvents);
        A.CallTo(() => _mediator.Send(A<QueryPayable>._, A<CancellationToken>._)).Returns(payableResult);
        A.CallTo(() => _mediator.Send(A<QueryProviderPay>._, A<CancellationToken>._)).Returns((Core.Data.Entities.ProviderPay)null);
        A.CallTo(() => _providerPayApi.SendProviderPayRequest(A<ProviderPayApiRequest>._)).Returns(apiResponse);

        await CreateSubject().Handle(request, new TestableMessageHandlerContext());

        _transactionSupplier.AssertCommit();
        A.CallTo(() => _publishObservability.Commit()).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<ExamStatusEvent>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(
                A<ExamStatusEvent>.That.Matches(
                    s => s.StatusCode == StatusCode.ProviderPayableEventReceived), A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => _mapper.Map<ProviderPayApiRequest>(A<SpirometryExam>._)).MustHaveHappened();
        A.CallTo(() => _providerPayApi.SendProviderPayRequest(A<ProviderPayApiRequest>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<SaveProviderPay>.That.Matches(e => e.PaymentId == paymentId), A<CancellationToken>._)).MustHaveHappened();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Handle_When_ProviderPayApi_Returns_202_But_PaymentId_Id_Missing_Throws_Exception(string paymentId)
    {
        var exam = CreateExamWithStatus(Core.Data.Entities.StatusCode.SpirometryExamPerformed);
        var apiResponseBody = A.Fake<ProviderPayApiResponse>();
        apiResponseBody.PaymentId = paymentId;
        var apiResponse = new ApiResponse<ProviderPayApiResponse>(new HttpResponseMessage(HttpStatusCode.Accepted),
            apiResponseBody, null!);
        var request = A.Fake<ProcessProviderPay>();
        var unprocessedEvents = A.Fake<List<CdiEventForPayment>>();
        unprocessedEvents.Add(new CdiEventForPayment
        {
            EvaluationId = 123,
            EventType = nameof(CDIPassedEvent)
        });
        var payableResult = A.Fake<QueryPayableResult>(o => o.WithArgumentsForConstructor(new object[] { true }));
        A.CallTo(() => _mediator.Send(A<QuerySpirometryExam>._, A<CancellationToken>._)).Returns(exam);
        A.CallTo(() => _mediator.Send(A<QueryUnprocessedCdiEventForPayments>._, A<CancellationToken>._)).Returns(unprocessedEvents);
        A.CallTo(() => _mediator.Send(A<QueryPayable>._, A<CancellationToken>._)).Returns(payableResult);
        A.CallTo(() => _mediator.Send(A<QueryProviderPay>._, A<CancellationToken>._)).Returns((Core.Data.Entities.ProviderPay)null);
        A.CallTo(() => _providerPayApi.SendProviderPayRequest(A<ProviderPayApiRequest>._)).Returns(apiResponse);

        await Assert.ThrowsAsync<ProviderPayRequestException>(async () => await CreateSubject().Handle(request, new TestableMessageHandlerContext()));

        _transactionSupplier.AssertRollback();
        A.CallTo(() => _publishObservability.Commit()).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<ExamStatusEvent>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(
                A<ExamStatusEvent>.That.Matches(
                    s => s.StatusCode == StatusCode.ProviderPayableEventReceived), A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => _mapper.Map<ProviderPayApiRequest>(A<SpirometryExam>._)).MustHaveHappened();
        A.CallTo(() => _providerPayApi.SendProviderPayRequest(A<ProviderPayApiRequest>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<SaveProviderPay>.That.Matches(e => e.PaymentId == paymentId), A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Handle_When_ProviderPayApi_Returns_303_GetsPaymentId_And_Invokes_SaveProviderPay(string paymentId)
    {
        var exam = CreateExamWithStatus(Core.Data.Entities.StatusCode.SpirometryExamPerformed);
        var content = A.Fake<ProviderPayApiResponse>();
        content.PaymentId = paymentId;
        var apiResponse = new ApiResponse<ProviderPayApiResponse>(new HttpResponseMessage(HttpStatusCode.SeeOther),
            null, null!);
        apiResponse.Headers.Location = new Uri($"http://blah/{paymentId}");
        var request = A.Fake<ProcessProviderPay>();
        var unprocessedEvents = A.Fake<List<CdiEventForPayment>>();
        unprocessedEvents.Add(new CdiEventForPayment
        {
            EvaluationId = 123,
            EventType = nameof(CDIPassedEvent)
        });
        var payableResult = A.Fake<QueryPayableResult>(o => o.WithArgumentsForConstructor(new object[] { true }));
        A.CallTo(() => _mediator.Send(A<QuerySpirometryExam>._, A<CancellationToken>._)).Returns(exam);
        A.CallTo(() => _mediator.Send(A<QueryUnprocessedCdiEventForPayments>._, A<CancellationToken>._)).Returns(unprocessedEvents);
        A.CallTo(() => _mediator.Send(A<QueryPayable>._, A<CancellationToken>._)).Returns(payableResult);
        A.CallTo(() => _mediator.Send(A<QueryProviderPay>._, A<CancellationToken>._)).Returns((Core.Data.Entities.ProviderPay)null);
        A.CallTo(() => _providerPayApi.SendProviderPayRequest(A<ProviderPayApiRequest>._)).Returns(apiResponse);

        await Assert.ThrowsAsync<ProviderPayRequestException>(async () => await CreateSubject().Handle(request, new TestableMessageHandlerContext()));

        _transactionSupplier.AssertRollback();
        A.CallTo(() => _publishObservability.Commit()).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<ExamStatusEvent>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(
                A<ExamStatusEvent>.That.Matches(
                    s => s.StatusCode == StatusCode.ProviderPayableEventReceived), A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => _mapper.Map<ProviderPayApiRequest>(A<SpirometryExam>._)).MustHaveHappened();
        A.CallTo(() => _providerPayApi.SendProviderPayRequest(A<ProviderPayApiRequest>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<SaveProviderPay>._, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Theory]
    [InlineData(nameof(CDIPassedEvent), true)]
    [InlineData(nameof(CDIFailedEvent), true)]
    public async Task Handle_When_ProviderPayApi_Success_But_SaveProviderPay_ThrowError_DoesNot_CommitTransaction(string eventType, bool payProvider)
    {
        var exam = CreateExamWithStatus(Core.Data.Entities.StatusCode.SpirometryExamPerformed);
        var paymentId = Guid.NewGuid().ToString();
        var content = A.Fake<ProviderPayApiResponse>();
        content.PaymentId = paymentId;
        var apiResponse = new ApiResponse<ProviderPayApiResponse>(new HttpResponseMessage(HttpStatusCode.SeeOther),
            null, null!);
        apiResponse.Headers.Location = new Uri($"http://blah/{paymentId}");
        var request = A.Fake<ProcessProviderPay>();
        var unprocessedEvents = A.Fake<List<CdiEventForPayment>>();
        unprocessedEvents.Add(new CdiEventForPayment
        {
            EvaluationId = 123,
            EventType = eventType,
            PayProvider = payProvider
        });
        var payableResult = A.Fake<QueryPayableResult>(o => o.WithArgumentsForConstructor(new object[] { true }));
        A.CallTo(() => _mediator.Send(A<QuerySpirometryExam>._, A<CancellationToken>._)).Returns(exam);
        A.CallTo(() => _mediator.Send(A<QueryUnprocessedCdiEventForPayments>._, A<CancellationToken>._)).Returns(unprocessedEvents);
        A.CallTo(() => _mediator.Send(A<QueryPayable>._, A<CancellationToken>._)).Returns(payableResult);
        A.CallTo(() => _mediator.Send(A<QueryProviderPay>._, A<CancellationToken>._)).Returns((Core.Data.Entities.ProviderPay)null);
        A.CallTo(() => _providerPayApi.SendProviderPayRequest(A<ProviderPayApiRequest>._)).Returns(apiResponse);
        A.CallTo(() => _mediator.Send(A<SaveProviderPay>._, A<CancellationToken>._)).Throws<Exception>();

        await Assert.ThrowsAnyAsync<Exception>(async () => await CreateSubject().Handle(request, new TestableMessageHandlerContext()));

        _transactionSupplier.AssertRollback();
        A.CallTo(() => _publishObservability.Commit()).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<ExamStatusEvent>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(
                A<ExamStatusEvent>.That.Matches(
                    s => s.StatusCode == StatusCode.ProviderPayableEventReceived), A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => _mapper.Map<ProviderPayApiRequest>(A<SpirometryExam>._)).MustHaveHappened();
        A.CallTo(() => _providerPayApi.SendProviderPayRequest(A<ProviderPayApiRequest>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<SaveProviderPay>.That.Matches(e => e.PaymentId == paymentId), A<CancellationToken>._)).MustHaveHappened();
    }

    /// <summary>
    /// Creates an exam with specified Status Code
    /// </summary>
    /// <param name="statusCode"></param>
    /// <returns></returns>
    private static SpirometryExam CreateExamWithStatus(Core.Data.Entities.StatusCode statusCode)
    {
        return new SpirometryExam
        {
            ExamStatuses = new List<ExamStatus>
            {
                new()
                {
                    StatusCodeId = statusCode.StatusCodeId
                }
            }
        };
    }
}