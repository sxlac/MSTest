using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NsbEventHandlers;
using NServiceBus.Testing;
using Signify.DEE.Svc.Core.ApiClient;
using Signify.DEE.Svc.Core.Commands;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Events.Status;
using Signify.DEE.Svc.Core.Events;
using Signify.DEE.Svc.Core.Infrastructure;
using Signify.DEE.Svc.Core.Messages.Queries;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;
using Signify.Dps.Observability.Library.Services;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.EventHandlers.Nsb;

public class CdiFailedEventHandlerTest
{
    private readonly FakeTransactionSupplier _transactionSupplier = new();
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();
    private readonly IApplicationTime _applicationTime = A.Fake<FakeApplicationTime>();
    private readonly IEvaluationApi _evaluationApi = A.Fake<IEvaluationApi>();

    private CdiFailedEventHandler CreateSubject()
        => new(A.Dummy<ILogger<CdiFailedEventHandler>>(), _transactionSupplier, _mediator, _mapper, _publishObservability, _evaluationApi, _applicationTime);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_WhenNotPerformed_DoesNothing(bool payProvider)
    {
        // Arrange
        var request = new CDIFailedEvent
        {
            DateTime = _applicationTime.UtcNow(),
            PayProvider = payProvider
        };

        A.CallTo(() => _mediator.Send(A<GetExamByEvaluation>._, A<CancellationToken>._))
            .Returns(new Exam
            {
                ExamStatuses = new List<ExamStatus>
                {
                    new()
                    {
                        ExamStatusCodeId = ExamStatusCode.NotPerformed.ExamStatusCodeId
                    }
                }
            });

        var context = new TestableMessageHandlerContext();

        // Act
        await CreateSubject().Handle(request, context);

        // Assert
        _transactionSupplier.AssertRollback();
        A.CallTo(() => _mediator.Send(A<ExamStatusEvent>._, A<CancellationToken>._)).MustNotHaveHappened();
        Assert.Empty(context.SentMessages);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_WhenNo_PerformedAndNotPerformed_ThrowsException(bool payProvider)
    {
        // Arrange
        // Arrange
        var request = new CDIFailedEvent
        {
            DateTime = _applicationTime.UtcNow(),
            PayProvider = payProvider
        };

        A.CallTo(() => _mediator.Send(A<GetExamByEvaluation>._, A<CancellationToken>._))
            .Returns(new Exam
            {
                ExamStatuses = new List<ExamStatus>
                {
                    new()
                    {
                        ExamStatusCodeId = ExamStatusCode.ExamCreated.ExamStatusCodeId
                    }
                }
            });

        var context = new TestableMessageHandlerContext();

        // Act and Assert
        await Assert.ThrowsAnyAsync<Exception>(async () => await CreateSubject().Handle(request, context));
        _transactionSupplier.AssertRollback();
        Assert.Empty(context.SentMessages);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public async Task Handle_WhenPerformed_And_SatisfiesRules_PublishesEvents(bool payProvider, int count)
    {
        // Arrange
        int? evaluationId = 1;
        var eventDateTime = _applicationTime.UtcNow().AddDays(-1);
        var eventReceivedDateTime = _applicationTime.UtcNow();
        var request = new CDIFailedEvent
        {
            EvaluationId = evaluationId!.Value,
            DateTime = eventDateTime,
            PayProvider = payProvider,
            ReceivedByDeeDateTime = eventReceivedDateTime
        };
        var exam = new Exam
        {
            EvaluationId = evaluationId,
            ExamStatuses = new List<ExamStatus>
            {
                new()
                {
                    ExamStatusCodeId = ExamStatusCode.Performed.ExamStatusCodeId
                }
            }
        };
        A.CallTo(() => _mediator.Send(A<GetExamByEvaluation>._, A<CancellationToken>._))
            .Returns(exam);
        var context = new TestableMessageHandlerContext();
        var mappedProviderPayRequest = A.Fake<ProviderPayRequest>();
        mappedProviderPayRequest.ParentEventReceivedDateTime = eventReceivedDateTime;
        mappedProviderPayRequest.ParentEventDateTime = eventDateTime;
        mappedProviderPayRequest.ParentEvent = "CDIFailedEvent";
        mappedProviderPayRequest.EvaluationId = evaluationId!.Value;
        A.CallTo(() => _mapper.Map<ProviderPayRequest>(exam)).Returns(mappedProviderPayRequest);

        // Act
        await CreateSubject().Handle(request, context);

        // Assert
        _transactionSupplier.AssertCommit();
        A.CallTo(() => _publishObservability.Commit()).MustHaveHappenedOnceExactly();
        Assert.Equal(count, context.SentMessages.Length);

        var statusCode = payProvider ? ExamStatusCode.CdiFailedWithPayReceived : ExamStatusCode.CdiFailedWithoutPayReceived;

        A.CallTo(() => _mediator.Send(A<UpdateExamStatus>.That.Matches(s =>
                s.ExamStatus.StatusCode.ExamStatusCodeId == statusCode.ExamStatusCodeId),
            A<CancellationToken>._)).MustHaveHappenedOnceExactly();

        if (!payProvider)
        {
            A.CallTo(() => _mediator.Send(A<UpdateExamStatus>.That.Matches(s =>
                    s.ExamStatus.StatusCode.ExamStatusCodeId == ExamStatusCode.ProviderNonPayableEventReceived.ExamStatusCodeId &&
                    ((ProviderPayStatusEvent)s.ExamStatus).Reason != null),
                A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        }
        else
        {
            var providerPayRequest = context.FindSentMessage<ProviderPayRequest>();
            Assert.Equal(eventReceivedDateTime, providerPayRequest.ParentEventReceivedDateTime);
            Assert.Equal(eventDateTime, providerPayRequest.ParentEventDateTime);
            Assert.Equal(nameof(CDIFailedEvent), providerPayRequest.ParentEvent);
        }
    }
}