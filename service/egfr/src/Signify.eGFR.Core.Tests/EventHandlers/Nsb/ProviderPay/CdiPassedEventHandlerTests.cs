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
using Signify.eGFR.Core.ApiClients.EvaluationApi;
using Signify.eGFR.Core.Data.Entities;
using Signify.eGFR.Core.Events.Akka;
using Signify.eGFR.Core.Infrastructure;
using Signify.eGFR.Core.Models;
using Signify.eGFR.Core.Queries;
using Xunit;

namespace Signify.eGFR.Core.Tests.EventHandlers.Nsb.ProviderPay;

public class CdiPassedEventHandlerTests
{
    private readonly FakeTransactionSupplier _transactionSupplier = new();
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();
    private readonly IEvaluationApi _evaluationApi = A.Fake<IEvaluationApi>();
    private readonly IApplicationTime _applicationTime = new ApplicationTime();

    private CdiPassedEventHandler CreateSubject()
        => new(A.Dummy<ILogger<CdiPassedEventHandler>>(), _mediator, _transactionSupplier,
            _publishObservability, _applicationTime, _mapper, _evaluationApi);

    [Fact]
    public async Task Handle_WhenNotPerformed_DoesNothing()
    {
        // Arrange
        var request = new CDIPassedEvent
        {
            DateTime = DateTimeOffset.UtcNow
        };

        A.CallTo(() => _mediator.Send(A<QueryExamByEvaluation>._, A<CancellationToken>._))
            .Returns(new Exam
            {
                ExamStatuses = new List<ExamStatus>
                {
                    new()
                    {
                        ExamStatusCodeId = (int) StatusCode.ExamNotPerformed
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
}
