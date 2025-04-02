using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NsbEventHandlers;
using NServiceBus.Testing;
using Signify.DEE.Svc.Core.ApiClient;
using Signify.DEE.Svc.Core.Commands;
using Signify.DEE.Svc.Core.Data.Entities;
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

public class CdiPassedEventHandlerTest
{
    private readonly FakeTransactionSupplier _transactionSupplier = new();
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();
    private readonly IApplicationTime _applicationTime = A.Fake<FakeApplicationTime>();
    private readonly IEvaluationApi _evaluationApi = A.Fake<IEvaluationApi>();

    private CdiPassedEventHandler CreateSubject()
        => new(A.Dummy<ILogger<CdiPassedEventHandler>>(), _transactionSupplier, _mediator, _mapper, _publishObservability, _evaluationApi, _applicationTime);

    [Fact]
    public async Task Handle_WhenNotPerformed_DoesNothing()
    {
        // Arrange
        var request = new CDIPassedEvent
        {
            DateTime = _applicationTime.UtcNow()
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
        Assert.Empty(context.SentMessages);
    }

    [Fact]
    public async Task Handle_WhenNo_PerformedAndNotPerformed_ThrowsException()
    {
        // Arrange
        var request = new CDIPassedEvent
        {
            DateTime = _applicationTime.UtcNow()
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

    [Fact]
    public async Task Handle_WhenPerformed_PublishesEvents()
    {
        // Arrange
        int? evaluationId = 1;
        const int examId = 1;
        var exam = A.Fake<Exam>();
        exam.ExamId = examId;
        exam.EvaluationId = evaluationId;
        exam.ExamStatuses = new List<ExamStatus>
        {
            new()
            {
                ExamStatusCodeId = ExamStatusCode.Performed.ExamStatusCodeId
            }
        };
        var eventDateTime = _applicationTime.UtcNow().AddDays(-1);
        var eventReceivedDateTime = _applicationTime.UtcNow();
        var message = new CDIPassedEvent
        {
            EvaluationId = evaluationId!.Value,
            DateTime = eventDateTime,
            RequestId = Guid.NewGuid(),
            ReceivedByDeeDateTime = eventReceivedDateTime
        };
        A.CallTo(() => _mediator.Send(A<GetExamByEvaluation>._, A<CancellationToken>._))
            .Returns(exam);
        var context = new TestableMessageHandlerContext();
        var mappedProviderPayRequest = A.Fake<ProviderPayRequest>();
        mappedProviderPayRequest.ParentEventReceivedDateTime = eventReceivedDateTime;
        mappedProviderPayRequest.ParentEventDateTime = eventDateTime;
        mappedProviderPayRequest.ParentEvent = "CDIPassedEvent";
        mappedProviderPayRequest.EvaluationId = evaluationId!.Value;
        A.CallTo(() => _mapper.Map<ProviderPayRequest>(exam)).Returns(mappedProviderPayRequest);

        // Act
        await CreateSubject().Handle(message, context);

        // Assert
        _transactionSupplier.AssertCommit();
        A.CallTo(() => _publishObservability.Commit()).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<UpdateExamStatus>.That.Matches(s =>
                s.ExamStatus.StatusCode.ExamStatusCodeId == ExamStatusCode.CdiPassedReceived.ExamStatusCodeId),
            A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<GetExamStatusModel>.That.Matches(m => m.ExamId == exam.ExamId), A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        var providerPayRequest = context.FindSentMessage<ProviderPayRequest>();
        Assert.NotNull(providerPayRequest);
        Assert.NotEmpty(providerPayRequest.AdditionalDetails);
        Assert.Equal(eventReceivedDateTime, providerPayRequest.ParentEventReceivedDateTime);
        Assert.Equal(eventDateTime, providerPayRequest.ParentEventDateTime);
        Assert.Equal(nameof(CDIPassedEvent), providerPayRequest.ParentEvent);
    }
}