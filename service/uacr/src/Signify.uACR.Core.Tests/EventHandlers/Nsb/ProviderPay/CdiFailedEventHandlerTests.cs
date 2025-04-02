using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NsbEventHandlers;
using NServiceBus.Testing;
using Signify.Dps.Observability.Library.Services;
using Signify.uACR.Core.ApiClients.EvaluationApi;
using Signify.uACR.Core.Data.Entities;
using Signify.uACR.Core.Events.Akka;
using Signify.uACR.Core.Models;
using Signify.uACR.Core.Queries;
using Xunit;

namespace Signify.uACR.Core.Tests.EventHandlers.Nsb;

public class CdiFailedEventHandlerTests
{
    private readonly FakeTransactionSupplier _transactionSupplier = new();
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();
    private readonly IEvaluationApi _evaluationApi = A.Fake<IEvaluationApi>();
    private readonly FakeApplicationTime _applicationTime = new();

    private CdiFailedEventHandler CreateSubject()
        => new(A.Dummy<ILogger<CdiFailedEventHandler>>(), _transactionSupplier, _mediator, _mapper, _evaluationApi, _publishObservability, _applicationTime);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_When_RequestComesIn_CallsBaseHandler(bool payProvider)
    {
        // Arrange
        var request = new CDIFailedEvent
        {
            DateTime = DateTimeOffset.UtcNow,
            PayProvider = payProvider
        };
        A.CallTo(() => _mediator.Send(A<QueryExamByEvaluation>._, A<CancellationToken>._))
            .Returns(new Exam
            {
                ExamStatuses = new List<ExamStatus>
                {
                    new()
                    {
                        ExamStatusCodeId = (int)StatusCode.ExamPerformed
                    }
                }
            });
        var context = new TestableMessageHandlerContext();

        // Act
        await CreateSubject().Handle(request, context);

        // Assert
        A.CallTo(() => _mediator.Send(A<QueryExamByEvaluation>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }
}