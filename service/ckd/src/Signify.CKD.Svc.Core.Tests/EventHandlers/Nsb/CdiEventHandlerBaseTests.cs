using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NsbEventHandlers;
using NServiceBus;
using NServiceBus.Testing;
using Signify.CKD.Svc.Core.BusinessRules;
using Signify.CKD.Svc.Core.Commands;
using Signify.CKD.Svc.Core.Data;
using Signify.CKD.Svc.Core.Data.Entities;
using Signify.CKD.Svc.Core.Events;
using Signify.CKD.Svc.Core.Exceptions;
using Signify.CKD.Svc.Core.Infrastructure.Observability;
using Signify.CKD.Svc.Core.Models;
using Signify.CKD.Svc.Core.Queries;
using Xunit;

namespace Signify.CKD.Svc.Core.Tests.EventHandlers.Nsb;

public class CdiEventHandlerBaseTests
{
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();
    private readonly IPayableRules _payableRules = A.Fake<IPayableRules>();

    private class ConcreteSubject : CdiEventHandlerBase
    {
        public ConcreteSubject(IMediator mediator, IPublishObservability publishObservability, IPayableRules payableRules)
            : base(A.Dummy<ILogger>(), mediator, A.Dummy<IMapper>(), publishObservability, payableRules)
        {
        }

        public new Task<Core.Data.Entities.CKD> GetExam(CdiEventBase message)
            => base.GetExam(message);

        public new Task<bool> IsPerformed(Core.Data.Entities.CKD ckd)
            => base.IsPerformed(ckd);

        public new Task Handle(CdiEventBase message, Core.Data.Entities.CKD ckd, IMessageHandlerContext context)
            => base.Handle(message, ckd, context);
    }

    private ConcreteSubject CreateSubject()
        => new(_mediator, _publishObservability, _payableRules);

    [Fact]
    public async Task GetExam_WhenExists_ReturnsExam()
    {
        // Arrange
        const long evaluationId = 1;

        var request = new CDIPassedEvent
        {
            EvaluationId = evaluationId
        };

        A.CallTo(() => _mediator.Send(A<GetCKD>._, A<CancellationToken>._))
            .Returns(new Core.Data.Entities.CKD());

        // Act
        var actual = await CreateSubject().GetExam(request);

        // Assert
        Assert.NotNull(actual);

        A.CallTo(() => _mediator.Send(A<GetCKD>.That.Matches(q =>
                    q.EvaluationId == evaluationId),
                A<CancellationToken>._))
            .MustHaveHappened();
    }

    [Fact]
    public async Task GetExam_WhenNotExists_Throws()
    {
        // Arrange
        const long evaluationId = 1;

        var request = new CDIPassedEvent
        {
            EvaluationId = evaluationId
        };

        A.CallTo(() => _mediator.Send(A<GetCKD>._, A<CancellationToken>._))
            .Returns((Core.Data.Entities.CKD)null);

        // Act
        // Assert
        await Assert.ThrowsAsync<ExamNotFoundException>(async () => await CreateSubject().GetExam(request));
    }

    [Theory]
    [MemberData(nameof(IsPerformed_TestData))]
    public async Task IsPerformed_Tests(IEnumerable<CKDStatusCode> statuses, bool expectedResult)
    {
        // Arrange
        var ckd = new Core.Data.Entities.CKD();

        A.CallTo(() => _mediator.Send(A<GetCKDStatuses>._, A<CancellationToken>._))
            .Returns(statuses
                .Select(status => new CKDStatus
                {
                    CKDStatusCodeId = status.CKDStatusCodeId
                })
                .ToList());

        // Act
        var actual = await CreateSubject().IsPerformed(ckd);

        // Assert
        Assert.Equal(expectedResult, actual);
    }

    public static IEnumerable<object[]> IsPerformed_TestData()
    {
        yield return new object[]
        {
            new List<CKDStatusCode>
            {
                CKDStatusCode.CKDPerformed
            },
            true
        };

        yield return new object[]
        {
            new List<CKDStatusCode>
            {
                CKDStatusCode.BillableEventRecieved,
                CKDStatusCode.CKDPerformed
            },
            true
        };

        yield return new object[]
        {
            new List<CKDStatusCode>
            {
                CKDStatusCode.CKDNotPerformed
            },
            false
        };
    }

    [Fact]
    public async Task IsPerformed_WhenNoStatusExists_Throws()
    {
        // Arrange
        var ckd = new Core.Data.Entities.CKD();

        // Act
        // Assert
        await Assert.ThrowsAnyAsync<Exception>(async () => await CreateSubject().IsPerformed(ckd));
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

        var exam = new Core.Data.Entities.CKD();

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
        Assert.Equal(CKDStatusCode.ProviderNonPayableEventReceived, status.StatusCode);
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

        var exam = new Core.Data.Entities.CKD();

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
        Assert.Equal(CKDStatusCode.ProviderPayableEventReceived, status.StatusCode);
        Assert.Equal(nameof(CDIPassedEvent), status.ParentCdiEvent);
        Assert.Null(status.Reason);

        var providerPayRequest = context.FindSentMessage<ProviderPayRequest>();
        Assert.NotNull(providerPayRequest);

        Assert.NotEmpty(providerPayRequest.AdditionalDetails);

        A.CallTo(_publishObservability)
            .MustHaveHappened();
    }
}
