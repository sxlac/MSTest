using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using NServiceBus;
using Refit;
using Signify.uACR.Core.ApiClients.EvaluationApi.Responses;
using Signify.uACR.Core.ApiClients.EvaluationApi;
using Signify.uACR.Core.Commands;
using Signify.uACR.Core.Constants;
using Signify.uACR.Core.Data.Entities;
using Signify.uACR.Core.Data;
using Signify.uACR.Core.EventHandlers.Nsb;
using Signify.uACR.Core.Events.Akka;
using Signify.uACR.Core.Exceptions;
using Signify.uACR.Core.Infrastructure;
using Signify.uACR.Core.Models;
using Signify.uACR.Core.Queries;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using System;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using UacrNsbEvents;
using Xunit;

namespace Signify.uACR.Core.Tests.EventHandlers.Nsb;

public class CdiEventHandlerBaseTests
{
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();
    private readonly FakeTransactionSupplier _transactionSupplier = new();
    private readonly IEvaluationApi _evaluationApi = A.Fake<IEvaluationApi>();
    private readonly FakeApplicationTime _applicationTime = new();

    private class ConcreteSubject(
        IMediator mediator,
        ITransactionSupplier transactionSupplier,
        IEvaluationApi evaluationApi,
        IPublishObservability publishObservability,
        IApplicationTime applicationTime)
        : CdiEventHandlerBase(A.Dummy<ILogger>(), mediator, A.Dummy<IMapper>(), transactionSupplier, evaluationApi,
            publishObservability, applicationTime)
    {
        public Task<Exam> GetExam(CdiEventBase message)
            => base.GetExam(message, CancellationToken.None);

        public new static bool IsPerformed(Exam exam)
            => CdiEventHandlerBase.IsPerformed(exam);

        public new Task Handle(CdiEventBase message, ExamStatusCode statusCode, IMessageHandlerContext context)
            => base.Handle(message, statusCode, context);
    }

    private ConcreteSubject CreateSubject()
        => new(_mediator, _transactionSupplier, _evaluationApi, _publishObservability, _applicationTime);

    [Theory]
    [MemberData(nameof(CdiEventsCollection))]
    public async Task GetExam_WhenExists_ReturnsExam(CdiEventBase cdiEvent)
    {
        // Arrange
        const long evaluationId = 1;
        cdiEvent.EvaluationId = evaluationId;

        A.CallTo(() => _mediator.Send(A<QueryExamByEvaluation>._, A<CancellationToken>._))
            .Returns(new Exam());

        // Act
        var actual = await CreateSubject().GetExam(cdiEvent);

        // Assert
        Assert.NotNull(actual);
    }

    [Theory]
    [MemberData(nameof(CdiEventsCollection))]
    public async Task Handle_WhenExamNotFound_ButEvaluationFinalized_ThrowsException_And_PublishObservability(CdiEventBase cdiEvent)
    {
        var apiResponseBody = A.Fake<List<EvaluationStatusHistory>>();
        var status = A.Fake<EvaluationStatusHistory>();
        status.EvaluationStatusCodeId = EvaluationStatus.EvaluationFinalized;
        apiResponseBody.Add(status);
        var apiResponse = new ApiResponse<List<EvaluationStatusHistory>>(new HttpResponseMessage(HttpStatusCode.OK),
            apiResponseBody, null!);
        A.CallTo(() => _evaluationApi.GetEvaluationStatusHistory(A<long>._)).Returns(apiResponse);
        A.CallTo(() => _mediator.Send(A<QueryExamByEvaluation>._, A<CancellationToken>._)).Returns((Exam)null);

        var context = new TestableMessageHandlerContext();
        var cdiStatus = GetCdiStatus(cdiEvent);

        // Act & Assert
        await Assert.ThrowsAsync<ExamNotFoundException>(async () => await CreateSubject().Handle(cdiEvent, cdiStatus, context));
        _transactionSupplier.AssertRollback();
        Assert.Empty(context.SentMessages);
        A.CallTo(() => _publishObservability.RegisterEvent(
            A<ObservabilityEvent>.That.Matches(e => e.EventType == Observability.Evaluation.MissingEvaluationEvent), true)).MustHaveHappenedOnceExactly();
    }

    [Theory]
    [MemberData(nameof(CdiEventsCollection))]
    public async Task Handle_WhenExamNotFound_ButEvaluationCanceled_DoesNotThrowsException_But_PublishObservability(CdiEventBase cdiEvent)
    {
        var apiResponseBody = A.Fake<List<EvaluationStatusHistory>>();
        var status = A.Fake<EvaluationStatusHistory>();
        status.EvaluationStatusCodeId = EvaluationStatus.EvaluationCanceled;
        apiResponseBody.Add(status);
        var apiResponse = new ApiResponse<List<EvaluationStatusHistory>>(new HttpResponseMessage(HttpStatusCode.OK),
            apiResponseBody, null!);
        A.CallTo(() => _evaluationApi.GetEvaluationStatusHistory(A<long>._)).Returns(apiResponse);
        A.CallTo(() => _mediator.Send(A<QueryExamByEvaluation>._, A<CancellationToken>._)).Returns((Exam)null);
        var cdiStatus = GetCdiStatus(cdiEvent);

        var context = new TestableMessageHandlerContext();

        // Act
        await CreateSubject().Handle(cdiEvent, cdiStatus, context);

        //Assert
        _transactionSupplier.AssertCommit();
        Assert.Empty(context.SentMessages);
        A.CallTo(() => _publishObservability.RegisterEvent(
            A<ObservabilityEvent>.That.Matches(e => e.EventType == Observability.Evaluation.EvaluationCanceledEvent), true)).MustHaveHappenedOnceExactly();
    }

    [Theory]
    [MemberData(nameof(CdiEventsCollection))]
    public async Task Handle_WhenExamNotFound_ButEvaluationFinalizedAndCanceled_ThrowException_And_PublishObservability(CdiEventBase cdiEvent)
    {
        var apiResponseBody = A.Fake<List<EvaluationStatusHistory>>();
        var status1 = A.Fake<EvaluationStatusHistory>();
        status1.EvaluationStatusCodeId = EvaluationStatus.EvaluationFinalized;
        apiResponseBody.Add(status1);
        var status2 = A.Fake<EvaluationStatusHistory>();
        status2.EvaluationStatusCodeId = EvaluationStatus.EvaluationCanceled;
        apiResponseBody.Add(status2);
        var apiResponse = new ApiResponse<List<EvaluationStatusHistory>>(new HttpResponseMessage(HttpStatusCode.OK),
            apiResponseBody, null!);
        A.CallTo(() => _evaluationApi.GetEvaluationStatusHistory(A<long>._)).Returns(apiResponse);
        A.CallTo(() => _mediator.Send(A<QueryExamByEvaluation>._, A<CancellationToken>._)).Returns((Exam)null);
        var cdiStatus = GetCdiStatus(cdiEvent);

        var context = new TestableMessageHandlerContext();

        // Act & Assert
        await Assert.ThrowsAsync<ExamNotFoundException>(async () => await CreateSubject().Handle(cdiEvent, cdiStatus, context));

        _transactionSupplier.AssertRollback();
        Assert.Empty(context.SentMessages);
        A.CallTo(() => _publishObservability.RegisterEvent(
            A<ObservabilityEvent>.That.Matches(e => e.EventType == Observability.Evaluation.MissingEvaluationEvent), true)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _publishObservability.RegisterEvent(
            A<ObservabilityEvent>.That.Matches(e => e.EventType == Observability.Evaluation.EvaluationCanceledEvent), true)).MustHaveHappenedOnceExactly();
    }

    [Theory]
    [MemberData(nameof(CdiEventsCollection))]
    public async Task Handle_WhenExamNotFound_And_GetVersionHistoryApi_Returns_EmptyContent_ThrowException_And_PublishObservability(CdiEventBase cdiEvent)
    {
        var apiResponse = new ApiResponse<List<EvaluationStatusHistory>>(new HttpResponseMessage(HttpStatusCode.OK),
            null, null!);
        A.CallTo(() => _evaluationApi.GetEvaluationStatusHistory(A<long>._)).Returns(apiResponse);
        A.CallTo(() => _mediator.Send(A<QueryExamByEvaluation>._, A<CancellationToken>._)).Returns((Exam)null);
        var context = new TestableMessageHandlerContext();
        var cdiStatus = GetCdiStatus(cdiEvent);

        // Act & Assert
        await Assert.ThrowsAsync<EvaluationApiRequestException>(async () => await CreateSubject().Handle(cdiEvent, cdiStatus, context));

        _transactionSupplier.AssertRollback();
        Assert.Empty(context.SentMessages);
        A.CallTo(() => _publishObservability.RegisterEvent(
            A<ObservabilityEvent>.That.Matches(e => e.EventType == Observability.ApiIssues.ExternalApiFailureEvent), true)).MustHaveHappenedOnceExactly();
    }

    [Theory]
    [MemberData(nameof(CdiEventsCollectionWithErrorCodes), HttpStatusCode.InternalServerError)]
    [MemberData(nameof(CdiEventsCollectionWithErrorCodes), HttpStatusCode.Accepted)]
    [MemberData(nameof(CdiEventsCollectionWithErrorCodes), HttpStatusCode.MovedPermanently)]
    [MemberData(nameof(CdiEventsCollectionWithErrorCodes), HttpStatusCode.NotFound)]
    [MemberData(nameof(CdiEventsCollectionWithErrorCodes), HttpStatusCode.Conflict)]
    [MemberData(nameof(CdiEventsCollectionWithErrorCodes), HttpStatusCode.MethodNotAllowed)]
    [MemberData(nameof(CdiEventsCollectionWithErrorCodes), HttpStatusCode.Forbidden)]
    [MemberData(nameof(CdiEventsCollectionWithErrorCodes), HttpStatusCode.Unauthorized)]
    [MemberData(nameof(CdiEventsCollectionWithErrorCodes), HttpStatusCode.ServiceUnavailable)]
    public async Task Handle_WhenExamNotFound_And_GetVersionHistoryApi_Fails_ThrowException_And_PublishObservability(CdiEventBase cdiEvent,
        HttpStatusCode statusCode)
    {
        var apiResponse = new ApiResponse<List<EvaluationStatusHistory>>(new HttpResponseMessage(statusCode),
            null, null!);
        A.CallTo(() => _evaluationApi.GetEvaluationStatusHistory(A<long>._)).Returns(apiResponse);
        A.CallTo(() => _mediator.Send(A<QueryExamByEvaluation>._, A<CancellationToken>._)).Returns((Exam)null);
        var context = new TestableMessageHandlerContext();
        var cdiStatus = GetCdiStatus(cdiEvent);

        // Act & Assert
        await Assert.ThrowsAsync<EvaluationApiRequestException>(async () => await CreateSubject().Handle(cdiEvent, cdiStatus, context));

        _transactionSupplier.AssertRollback();
        Assert.Empty(context.SentMessages);
        A.CallTo(() => _publishObservability.RegisterEvent(
            A<ObservabilityEvent>.That.Matches(e => e.EventType == Observability.ApiIssues.ExternalApiFailureEvent), true)).MustHaveHappenedOnceExactly();
    }

    [Theory]
    [MemberData(nameof(CdiEventsCollection))]
    public async Task Handle_WritesStatusToDb_And_RaisesNsbForProviderPay_WhenLabResultsReceived(CdiEventBase cdiEvent)
    {
        // Arrange
        var exam = A.Fake<Exam>();
        exam.EvaluationId = 1;
        exam.ExamStatuses = new List<ExamStatus>
        {
            new()
            {
                ExamStatusCodeId = (int)StatusCode.ExamPerformed
            }
        };
        var context = new TestableMessageHandlerContext();
        A.CallTo(() => _mediator.Send(A<QueryExamByEvaluation>._, A<CancellationToken>._))
            .Returns(exam);
        A.CallTo(() => _mediator.Send(A<QueryLabResultByEvaluationId>._, A<CancellationToken>._)).Returns(A.Dummy<LabResult>());
        var cdiStatus = GetCdiStatus(cdiEvent);
        // Act
        await CreateSubject().Handle(cdiEvent, cdiStatus, context);

        A.CallTo(() => _mediator.Send(A<UpdateExamStatus>.That.Matches(s => s.ExamStatus.StatusCode == cdiStatus), A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        var sentMessage = context.FindSentMessage<ProviderPayRequest>();
        if (cdiStatus == ExamStatusCode.CdiFailedWithoutPayReceived)
        {
            A.CallTo(() => _mediator.Send(A<QueryLabResultByEvaluationId>._, A<CancellationToken>._))
                .MustNotHaveHappened();
            Assert.Empty(context.SentMessages);
        }
        else
        {
            A.CallTo(() => _mediator.Send(A<QueryLabResultByEvaluationId>._, A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
            Assert.Single(context.SentMessages);
            Assert.NotNull(sentMessage);
        }

        _transactionSupplier.AssertCommit();
    }

    [Theory]
    [MemberData(nameof(CdiEventsCollection))]
    public async Task Handle_WritesStatusToDb_And_DoesNotRaiseNsbForProviderPay_WhenNoLabResultsReceived(CdiEventBase cdiEvent)
    {
        // Arrange
        var exam = A.Fake<Exam>();
        exam.EvaluationId = 1;
        exam.ExamStatuses = new List<ExamStatus>
        {
            new()
            {
                ExamStatusCodeId = (int)StatusCode.ExamPerformed
            }
        };
        var context = new TestableMessageHandlerContext();
        A.CallTo(() => _mediator.Send(A<QueryExamByEvaluation>._, A<CancellationToken>._))
            .Returns(exam);
        A.CallTo(() => _mediator.Send(A<QueryLabResultByEvaluationId>._, A<CancellationToken>._)).Returns(null as LabResult);
        var cdiStatus = GetCdiStatus(cdiEvent);

        // Act
        await CreateSubject().Handle(cdiEvent, cdiStatus, context);

        A.CallTo(() => _mediator.Send(A<UpdateExamStatus>.That.Matches(s => s.ExamStatus.StatusCode == cdiStatus), A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        if (cdiStatus == ExamStatusCode.CdiFailedWithoutPayReceived)
        {
            A.CallTo(() => _mediator.Send(A<QueryLabResultByEvaluationId>._, A<CancellationToken>._))
                .MustNotHaveHappened();
        }
        else
        {
            A.CallTo(() => _mediator.Send(A<QueryLabResultByEvaluationId>._, A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        Assert.Empty(context.SentMessages);
        _transactionSupplier.AssertCommit();
    }

    [Fact]
    public void IsPerformed_WhenNoStatusExists_Throws()
    {
        // Arrange
        var exam = new Exam();

        // Act
        // Assert
        Assert.Throws<InvalidOperationException>(() => ConcreteSubject.IsPerformed(exam));
    }

    [Theory]
    [MemberData(nameof(CdiEventsCollection))]
    public async Task Handle_WhenNotPerformed_DoesNothing(CdiEventBase cdiEvent)
    {
        // Arrange
        A.CallTo(() => _mediator.Send(A<QueryExamByEvaluation>._, A<CancellationToken>._))
            .Returns(new Exam
            {
                ExamStatuses = new List<ExamStatus>
                {
                    new()
                    {
                        ExamStatusCodeId = (int)StatusCode.ExamNotPerformed
                    }
                }
            });

        var context = new TestableMessageHandlerContext();
        var cdiStatus = GetCdiStatus(cdiEvent);

        // Act
        await CreateSubject().Handle(cdiEvent, cdiStatus, context);

        // Assert
        A.CallTo(() => _mediator.Send(A<QueryExamByEvaluation>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>._, A<bool>._)).MustNotHaveHappened();
        _transactionSupplier.AssertCommit();
        Assert.Empty(context.SentMessages);
    }

    /// <summary>
    /// Returns the ExamStatusCode for the corresponding CDI event
    /// </summary>
    /// <param name="cdiEvent"></param>
    /// <returns></returns>
    private static ExamStatusCode GetCdiStatus(CdiEventBase cdiEvent)
    {
        if (cdiEvent.GetType() == typeof(CDIPassedEvent))
        {
            return ExamStatusCode.CdiPassedReceived;
        }

        return ((CDIFailedEvent)cdiEvent).PayProvider ? ExamStatusCode.CdiFailedWithPayReceived : ExamStatusCode.CdiFailedWithoutPayReceived;
    }

    /// <summary>
    /// EventType with PayProvider (if applicable)
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<object[]> CdiEventsCollection()
    {
        yield return
        [
            new CDIPassedEvent
            {
                EvaluationId = 1,
                RequestId = Guid.NewGuid(),
                DateTime = DateTimeOffset.UtcNow
            }
        ];
        yield return
        [
            new CDIFailedEvent
            {
                PayProvider = true,
                EvaluationId = 1,
                RequestId = Guid.NewGuid(),
                DateTime = DateTimeOffset.UtcNow
            }
        ];
        yield return
        [
            new CDIFailedEvent
            {
                PayProvider = false,
                EvaluationId = 1,
                RequestId = Guid.NewGuid(),
                DateTime = DateTimeOffset.UtcNow
            }
        ];
    }

    public static IEnumerable<object[]> CdiEventsCollectionWithErrorCodes(HttpStatusCode httpStatusCode)
    {
        return CdiEventsCollection().Select(item => new[]
        {
            item[0], httpStatusCode
        });
    }
}