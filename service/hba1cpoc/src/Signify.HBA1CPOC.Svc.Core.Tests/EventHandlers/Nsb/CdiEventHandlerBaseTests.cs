using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NsbEventHandlers;
using NServiceBus.Testing;
using NServiceBus;
using Signify.HBA1CPOC.Messages.Events.Akka;
using Signify.HBA1CPOC.Messages.Events.Status;
using Signify.HBA1CPOC.Messages.Events;
using Signify.HBA1CPOC.Svc.Core.ApiClient;
using Signify.HBA1CPOC.Svc.Core.BusinessRules;
using Signify.HBA1CPOC.Svc.Core.Commands;
using Signify.HBA1CPOC.Svc.Core.Data.Entities;
using Signify.HBA1CPOC.Svc.Core.Infrastructure.Observability;
using Signify.HBA1CPOC.Svc.Core.Infrastructure;
using Signify.HBA1CPOC.Svc.Core.Models;
using Signify.HBA1CPOC.Svc.Core.Queries;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.HBA1CPOC.Svc.Core.Tests.EventHandlers.Nsb;

public class CdiEventHandlerBaseTests
{
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly IPayableRules _payableRules = A.Fake<IPayableRules>();

    private class ConcreteSubject : CdiEventHandlerBase
    {
        public ConcreteSubject(IMediator mediator, IPayableRules payableRules)
            : base(A.Dummy<ILogger>(), mediator, A.Dummy<IMapper>(), payableRules, A.Dummy<IPublishObservability>(), A.Dummy<IEvaluationApi>(), new ApplicationTime())
        {
        }

        public new Task<Core.Data.Entities.HBA1CPOC> GetExam(BaseCdiEvent message)
            => base.GetExam(message);

        public new Task<bool> IsPerformed(Core.Data.Entities.HBA1CPOC exam)
            => base.IsPerformed(exam);

        public new Task Handle(BaseCdiEvent message, Core.Data.Entities.HBA1CPOC exam, IMessageHandlerContext context)
            => base.Handle(message, exam, context);
    }

    private ConcreteSubject CreateSubject()
        => new(_mediator, _payableRules);

    [Fact]
    public async Task GetExam_WhenExists_ReturnsExam()
    {
        // Arrange
        const long evaluationId = 1;

        var request = new CDIPassedEvent
        {
            EvaluationId = evaluationId
        };

        A.CallTo(() => _mediator.Send(A<GetHBA1CPOC>._, A<CancellationToken>._))
            .Returns(new Core.Data.Entities.HBA1CPOC());

        // Act
        var actual = await CreateSubject().GetExam(request);

        // Assert
        Assert.NotNull(actual);

        A.CallTo(() => _mediator.Send(A<GetHBA1CPOC>.That.Matches(q =>
                    q.EvaluationId == evaluationId && !q.IncludeStatuses),
                A<CancellationToken>._))
            .MustHaveHappened();
    }

    [Fact]
    public async Task GetExam_WhenNotExists_ReturnsNull()
    {
        // Arrange
        const long evaluationId = 1;

        var request = new CDIPassedEvent
        {
            EvaluationId = evaluationId
        };

        A.CallTo(() => _mediator.Send(A<GetHBA1CPOC>._, A<CancellationToken>._))
            .Returns((Core.Data.Entities.HBA1CPOC)null);

        // Act
        // Assert
        Assert.Null(await CreateSubject().GetExam(request));
    }

    [Theory]
    [MemberData(nameof(IsPerformed_TestData))]
    public async Task IsPerformed_Tests(IEnumerable<HBA1CPOCStatusCode> statuses, bool expectedResult)
    {
        // Arrange
        var exam = new Core.Data.Entities.HBA1CPOC();

        A.CallTo(() => _mediator.Send(A<QueryExamStatuses>._, A<CancellationToken>._))
            .Returns(statuses
                .Select(status => new HBA1CPOCStatus
                {
                    HBA1CPOCStatusCodeId = status.HBA1CPOCStatusCodeId
                })
                .ToList());

        // Act
        var actual = await CreateSubject().IsPerformed(exam);

        // Assert
        Assert.Equal(expectedResult, actual);
    }

    public static IEnumerable<object[]> IsPerformed_TestData()
    {
        yield return new object[]
        {
            new List<HBA1CPOCStatusCode>
            {
                HBA1CPOCStatusCode.HBA1CPOCPerformed
            },
            true
        };

        yield return new object[]
        {
            new List<HBA1CPOCStatusCode>
            {
                HBA1CPOCStatusCode.BillableEventRecieved,
                HBA1CPOCStatusCode.HBA1CPOCPerformed
            },
            true
        };

        yield return new object[]
        {
            new List<HBA1CPOCStatusCode>
            {
                HBA1CPOCStatusCode.HBA1CPOCNotPerformed
            },
            false
        };
    }

    [Fact]
    public async Task IsPerformed_WhenNoStatusExists_Throws()
    {
        // Arrange
        var exam = new Core.Data.Entities.HBA1CPOC();

        // Act
        // Assert
        await Assert.ThrowsAnyAsync<Exception>(async () => await CreateSubject().IsPerformed(exam));
    }

    [Fact]
    public async Task Handle_WhenNotPayable_PublishesProviderNonPayableEventReceived()
    {
        // Arrange
        const string reason = nameof(reason);

        var request = new CDIPassedEvent
        {
            EvaluationId = 1,
            RequestId = Guid.NewGuid(),
            DateTime = DateTimeOffset.UtcNow
        };

        var exam = new Core.Data.Entities.HBA1CPOC();

        A.CallTo(() => _payableRules.IsPayable(A<PayableRuleAnswers>._))
            .Returns(new BusinessRuleStatus
            {
                IsMet = false,
                Reason = reason
            });

        var context = new TestableMessageHandlerContext();

        // Act
        await CreateSubject().Handle(request, exam, context);

        // Assert
        Assert.Single(context.SentMessages);

        var status = context.FindSentMessage<ProviderPayStatusEvent>();
        Assert.NotNull(status);

        Assert.Equal(request.EvaluationId, status.EvaluationId);
        Assert.Equal(request.RequestId, status.EventId);
        Assert.Equal(HBA1CPOCStatusCode.ProviderNonPayableEventReceived.HBA1CPOCStatusCodeId, status.StatusCode);
        Assert.Equal(nameof(CDIPassedEvent), status.ParentCdiEvent);
        Assert.Equal(reason, status.Reason);
    }

    [Fact]
    public async Task Handle_WhenPayable_PublishesProviderPayableEventReceived()
    {
        // Arrange
        var request = new CDIPassedEvent
        {
            EvaluationId = 1,
            RequestId = Guid.NewGuid(),
            DateTime = DateTimeOffset.UtcNow
        };

        var exam = new Core.Data.Entities.HBA1CPOC();

        A.CallTo(() => _payableRules.IsPayable(A<PayableRuleAnswers>._))
            .Returns(new BusinessRuleStatus
            {
                IsMet = true
            });

        var context = new TestableMessageHandlerContext();

        // Act
        await CreateSubject().Handle(request, exam, context);

        // Assert
        Assert.Equal(2, context.SentMessages.Length);

        var status = context.FindSentMessage<ProviderPayStatusEvent>();
        Assert.NotNull(status);

        Assert.Equal(request.EvaluationId, status.EvaluationId);
        Assert.Equal(request.RequestId, status.EventId);
        Assert.Equal(HBA1CPOCStatusCode.ProviderPayableEventReceived.HBA1CPOCStatusCodeId, status.StatusCode);
        Assert.Equal(nameof(CDIPassedEvent), status.ParentCdiEvent);
        Assert.Null(status.Reason);

        var providerPayRequest = context.FindSentMessage<ProviderPayRequest>();
        Assert.NotNull(providerPayRequest);

        Assert.NotEmpty(providerPayRequest.AdditionalDetails);
    }
}
