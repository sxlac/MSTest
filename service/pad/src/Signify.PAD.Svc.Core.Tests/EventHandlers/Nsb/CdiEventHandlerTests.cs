using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NsbEventHandlers;
using NServiceBus.Testing;
using Refit;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using Signify.PAD.Svc.Core.ApiClient.Response;
using Signify.PAD.Svc.Core.ApiClient;
using Signify.PAD.Svc.Core.BusinessRules;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Constants;
using Signify.PAD.Svc.Core.Events;
using Signify.PAD.Svc.Core.Exceptions;
using Signify.PAD.Svc.Core.Infrastructure;
using Signify.PAD.Svc.Core.Models;
using Signify.PAD.Svc.Core.Queries;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.EventHandlers.Nsb;

public class CdiEventHandlerTests
{
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly TestableInvokeHandlerContext _messageSession = new();
    private readonly IPayableRules _payableRules = A.Fake<IPayableRules>();
    private readonly IEvaluationApi _evaluationApi = A.Fake<IEvaluationApi>();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();
    private readonly IApplicationTime _applicationTime = new ApplicationTime();

    private CdiEventHandler CreateSubject() => new(A.Dummy<ILogger<CdiEventHandler>>(), _mediator, _mapper, _payableRules, _evaluationApi,
        _publishObservability,
        _applicationTime);

    [Theory]
    [MemberData(nameof(Handle_Exam_NotFound_TestData))]
    public async Task Handle_Exam_NotFound_When_Only_Finalized(CdiEventBase cdiEvent)
    {
        var apiResponseBody = A.Fake<List<EvaluationStatusHistory>>();
        var status = A.Fake<EvaluationStatusHistory>();
        status.EvaluationStatusCodeId = EvaluationStatus.EvaluationFinalized;
        apiResponseBody.Add(status);
        var apiResponse = new ApiResponse<List<EvaluationStatusHistory>>(new HttpResponseMessage(HttpStatusCode.OK),
            apiResponseBody, null!);
        var subject = CreateSubject();
        A.CallTo(() => _mediator.Send(A<GetPAD>._, A<CancellationToken>._)).Returns(null as Core.Data.Entities.PAD);
        A.CallTo(() => _evaluationApi.GetEvaluationStatusHistory(A<long>._)).Returns(apiResponse);

        if (cdiEvent.GetType() == typeof(CDIPassedEvent))
        {
            await Assert.ThrowsAnyAsync<ExamNotFoundException>(async () => await subject.Handle((CDIPassedEvent)cdiEvent, _messageSession));
        }
        else
        {
            await Assert.ThrowsAnyAsync<ExamNotFoundException>(async () => await subject.Handle((CDIFailedEvent)cdiEvent, _messageSession));
        }

        A.CallTo(() => _publishObservability.RegisterEvent(
            A<ObservabilityEvent>.That.Matches(e => e.EventType == Observability.Evaluation.MissingEvaluationEvent), true)).MustHaveHappenedOnceExactly();
    }

    [Theory]
    [MemberData(nameof(Handle_Exam_NotFound_TestData))]
    public async Task Handle_Exam_NotFound_When_Only_Canceled(CdiEventBase cdiEvent)
    {
        var apiResponseBody = A.Fake<List<EvaluationStatusHistory>>();
        var status = A.Fake<EvaluationStatusHistory>();
        status.EvaluationStatusCodeId = EvaluationStatus.EvaluationCanceled;
        apiResponseBody.Add(status);
        var apiResponse = new ApiResponse<List<EvaluationStatusHistory>>(new HttpResponseMessage(HttpStatusCode.OK),
            apiResponseBody, null!);
        var subject = CreateSubject();
        A.CallTo(() => _mediator.Send(A<GetPAD>._, A<CancellationToken>._)).Returns(null as Core.Data.Entities.PAD);
        A.CallTo(() => _evaluationApi.GetEvaluationStatusHistory(A<long>._)).Returns(apiResponse);

        if (cdiEvent.GetType() == typeof(CDIPassedEvent))
        {
            await subject.Handle((CDIPassedEvent)cdiEvent, _messageSession);
        }
        else
        {
            await subject.Handle((CDIFailedEvent)cdiEvent, _messageSession);
        }

        A.CallTo(() => _publishObservability.RegisterEvent(
            A<ObservabilityEvent>.That.Matches(e => e.EventType == Observability.Evaluation.EvaluationCanceledEvent), true)).MustHaveHappenedOnceExactly();
    }

    [Theory]
    [MemberData(nameof(Handle_Exam_NotFound_TestData))]
    public async Task Handle_Exam_NotFound_When_Both_Finalized_And_Canceled(CdiEventBase cdiEvent)
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
        var subject = CreateSubject();
        A.CallTo(() => _mediator.Send(A<GetPAD>._, A<CancellationToken>._)).Returns(null as Core.Data.Entities.PAD);
        A.CallTo(() => _evaluationApi.GetEvaluationStatusHistory(A<long>._)).Returns(apiResponse);

        if (cdiEvent.GetType() == typeof(CDIPassedEvent))
        {
            await Assert.ThrowsAnyAsync<ExamNotFoundException>(async () => await subject.Handle((CDIPassedEvent)cdiEvent, _messageSession));
        }
        else
        {
            await Assert.ThrowsAnyAsync<ExamNotFoundException>(async () => await subject.Handle((CDIFailedEvent)cdiEvent, _messageSession));
        }

        A.CallTo(() => _publishObservability.RegisterEvent(
            A<ObservabilityEvent>.That.Matches(e => e.EventType == Observability.Evaluation.MissingEvaluationEvent), true)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _publishObservability.RegisterEvent(
            A<ObservabilityEvent>.That.Matches(e => e.EventType == Observability.Evaluation.EvaluationCanceledEvent), true)).MustHaveHappenedOnceExactly();
    }

    [Theory]
    [MemberData(nameof(Handle_Exam_NotFound_TestData))]
    public async Task Handle_Exam_NotFound_When_GetVersionHistoryApi_Fails(CdiEventBase cdiEvent)
    {
        var apiResponse = new ApiResponse<List<EvaluationStatusHistory>>(new HttpResponseMessage(HttpStatusCode.InternalServerError),
            null, null!);
        var subject = CreateSubject();
        A.CallTo(() => _mediator.Send(A<GetPAD>._, A<CancellationToken>._)).Returns(null as Core.Data.Entities.PAD);
        A.CallTo(() => _evaluationApi.GetEvaluationStatusHistory(A<long>._)).Returns(apiResponse);

        if (cdiEvent.GetType() == typeof(CDIPassedEvent))
        {
            await Assert.ThrowsAnyAsync<EvaluationApiRequestException>(async () => await subject.Handle((CDIPassedEvent)cdiEvent, _messageSession));
        }
        else
        {
            await Assert.ThrowsAnyAsync<EvaluationApiRequestException>(async () => await subject.Handle((CDIFailedEvent)cdiEvent, _messageSession));
        }

        A.CallTo(() => _publishObservability.RegisterEvent(
                A<ObservabilityEvent>.That.Matches(e =>
                    e.EventType == Observability.ApiIssues.ExternalApiFailureEvent && e.EventValue["Message"].ToString().Contains("failed with HttpStatusCode:")),
                true))
            .MustHaveHappenedOnceExactly();
    }

    [Theory]
    [MemberData(nameof(Handle_Exam_NotFound_TestData))]
    public async Task Handle_Exam_NotFound_When_GetVersionHistoryApi_Returns_EmptyContent(CdiEventBase cdiEvent)
    {
        var apiResponse = new ApiResponse<List<EvaluationStatusHistory>>(new HttpResponseMessage(HttpStatusCode.OK),
            null, null!);
        var subject = CreateSubject();
        A.CallTo(() => _mediator.Send(A<GetPAD>._, A<CancellationToken>._)).Returns(null as Core.Data.Entities.PAD);
        A.CallTo(() => _evaluationApi.GetEvaluationStatusHistory(A<long>._)).Returns(apiResponse);

        if (cdiEvent.GetType() == typeof(CDIPassedEvent))
        {
            await Assert.ThrowsAnyAsync<EvaluationApiRequestException>(async () => await subject.Handle((CDIPassedEvent)cdiEvent, _messageSession));
        }
        else
        {
            await Assert.ThrowsAnyAsync<EvaluationApiRequestException>(async () => await subject.Handle((CDIFailedEvent)cdiEvent, _messageSession));
        }

        A.CallTo(() => _publishObservability.RegisterEvent(
                A<ObservabilityEvent>.That.Matches(e =>
                    e.EventType == Observability.ApiIssues.ExternalApiFailureEvent && e.EventValue["Message"].ToString().Contains("Empty response from")),
                true))
            .MustHaveHappenedOnceExactly();
    }

    [Theory]
    [MemberData(nameof(Handle_Exam_NotFound_TestData))]
    public async Task Handle_Exam_Found_PadPerformed_NotFound(CdiEventBase cdiEvent)
    {
        var pad = new Core.Data.Entities.PAD
        {
            PADId = 1
        };
        A.CallTo(() => _mediator.Send(A<GetPAD>._, A<CancellationToken>._)).Returns(pad);
        A.CallTo(() => _mediator.Send(A<QueryPadPerformedStatus>._, A<CancellationToken>._)).Returns(new QueryPadPerformedStatusResult(false));
        var subject = CreateSubject();
        if (cdiEvent.GetType() == typeof(CDIPassedEvent))
        {
            await subject.Handle((CDIPassedEvent)cdiEvent, _messageSession);
        }
        else
        {
            await subject.Handle((CDIFailedEvent)cdiEvent, _messageSession);
        }

        _messageSession.SentMessages.Length.Should().Be(0);
    }

    public static IEnumerable<object[]> Handle_Exam_NotFound_TestData()
    {
        return CdiEventsCollectionOnSuccess().Select(result => result.Take(1).ToArray());
    }

    [Theory]
    [MemberData(nameof(CdiEventsCollectionOnSuccess))]
    public async Task Handle_Exam_Found_StatusWrite_KafkaPublish_Success_WhenRulesAre_Met(CdiEventBase cdiEvent, int statusCode, int messageCount)
    {
        var pad = new Core.Data.Entities.PAD
        {
            PADId = 1
        };
        var rulesCheckResult = A.Fake<BusinessRuleStatus>();
        rulesCheckResult.IsMet = true;
        A.CallTo(() => _mediator.Send(A<GetPAD>._, A<CancellationToken>._)).Returns(pad);
        A.CallTo(() => _mediator.Send(A<QueryPadPerformedStatus>._, A<CancellationToken>._)).Returns(new QueryPadPerformedStatusResult(true));
        A.CallTo(() => _payableRules.IsPayable(A<PayableRuleAnswers>._)).Returns(rulesCheckResult);
        var subject = CreateSubject();
        string eventType;
        if (cdiEvent.GetType() == typeof(CDIPassedEvent))
        {
            await subject.Handle((CDIPassedEvent)cdiEvent, _messageSession);
            eventType = nameof(CDIPassedEvent);
        }
        else
        {
            await subject.Handle((CDIFailedEvent)cdiEvent, _messageSession);
            eventType = nameof(CDIFailedEvent);
        }

        _messageSession.SentMessages.Length.Should().Be(messageCount);
        _messageSession.FindSentMessage<ProviderPayStatusEvent>().Should().NotBeNull();
        (_messageSession.SentMessages[0].Message as ProviderPayStatusEvent)!.ParentCdiEvent.Should().Be(eventType);
        (_messageSession.SentMessages[0].Message as ProviderPayStatusEvent)!.StatusCode.PADStatusCodeId.Should().Be(statusCode);
        if (cdiEvent is CDIFailedEvent { PayProvider: false })
        {
            _messageSession.FindSentMessage<ProviderPayRequest>().Should().BeNull();
        }
        else
        {
            _messageSession.FindSentMessage<ProviderPayRequest>().Should().NotBeNull();
            _messageSession.FindSentMessage<ProviderPayRequest>().AdditionalDetails.Count.Should().Be(3);
            (_messageSession.SentMessages[1].Message as ProviderPayStatusEvent)!.ParentCdiEvent.Should().Be(eventType);
            (_messageSession.SentMessages[1].Message as ProviderPayStatusEvent)!.StatusCode.PADStatusCodeId.Should()
                .Be((int)StatusCodes.ProviderPayableEventReceived);
        }
    }

    [Theory]
    [MemberData(nameof(CdiEventsCollectionOnFailure))]
    public async Task Handle_Exam_Found_StatusWrite_KafkaPublish_Success_WhenRulesAre_NotMet(CdiEventBase cdiEvent, int statusCode, int messageCount)
    {
        var pad = new Core.Data.Entities.PAD
        {
            PADId = 1
        };
        var rulesCheckResult = A.Fake<BusinessRuleStatus>();
        rulesCheckResult.IsMet = false;
        A.CallTo(() => _mediator.Send(A<GetPAD>._, A<CancellationToken>._)).Returns(pad);
        A.CallTo(() => _mediator.Send(A<QueryPadPerformedStatus>._, A<CancellationToken>._)).Returns(new QueryPadPerformedStatusResult(true));
        A.CallTo(() => _payableRules.IsPayable(A<PayableRuleAnswers>._)).Returns(rulesCheckResult);
        var subject = CreateSubject();
        string eventType;
        if (cdiEvent.GetType() == typeof(CDIPassedEvent))
        {
            await subject.Handle((CDIPassedEvent)cdiEvent, _messageSession);
            eventType = nameof(CDIPassedEvent);
        }
        else
        {
            await subject.Handle((CDIFailedEvent)cdiEvent, _messageSession);
            eventType = nameof(CDIFailedEvent);
        }

        _messageSession.SentMessages.Length.Should().Be(messageCount);
        _messageSession.FindSentMessage<ProviderPayStatusEvent>().Should().NotBeNull();
        (_messageSession.SentMessages[0].Message as ProviderPayStatusEvent)!.ParentCdiEvent.Should().Be(eventType);
        (_messageSession.SentMessages[0].Message as ProviderPayStatusEvent)!.StatusCode.PADStatusCodeId.Should().Be(statusCode);
        _messageSession.FindSentMessage<ProviderPayRequest>().Should().BeNull();
    }

    /// <summary>
    /// EventType with PayProvider (if applicable), StatusCode, Total number of sent Nsb messages expected
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<object[]> CdiEventsCollectionOnSuccess()
    {
        yield return [new CDIPassedEvent(), StatusCodes.CdiPassedReceived, 3];
        yield return [new CDIFailedEvent { PayProvider = true }, StatusCodes.CdiFailedWithPayReceived, 3];
        yield return [new CDIFailedEvent { PayProvider = false }, StatusCodes.CdiFailedWithoutPayReceived, 2];
    }

    /// <summary>
    /// EventType with PayProvider (if applicable), StatusCode, Total number of sent Nsb messages expected
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<object[]> CdiEventsCollectionOnFailure()
    {
        yield return [new CDIPassedEvent(), StatusCodes.CdiPassedReceived, 2];
        yield return [new CDIFailedEvent { PayProvider = true }, StatusCodes.CdiFailedWithPayReceived, 2];
        yield return [new CDIFailedEvent { PayProvider = false }, StatusCodes.CdiFailedWithoutPayReceived, 2];
    }
}