using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NsbEventHandlers;
using NServiceBus.Testing;
using Signify.CKD.Svc.Core.BusinessRules;
using Signify.CKD.Svc.Core.Data.Entities;
using Signify.CKD.Svc.Core.Events;
using Signify.CKD.Svc.Core.Infrastructure.Observability;
using Signify.CKD.Svc.Core.Queries;
using Xunit;

namespace Signify.CKD.Svc.Core.Tests.EventHandlers.Nsb
{
    public class CdiFailedEventHandlerTests
    {
        private readonly FakeTransactionSupplier _transactionSupplier = new();
        private readonly IMediator _mediator = A.Fake<IMediator>();
        private readonly IMapper _mapper = A.Fake<IMapper>();
        private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();
        private readonly IPayableRules _payableRules = A.Fake<IPayableRules>();

        private CdiFailedEventHandler CreateSubject()
            => new(A.Dummy<ILogger<CdiFailedEventHandler>>(), _transactionSupplier, _mediator, _mapper, _publishObservability, _payableRules);

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Handle_WhenNotPerformed_DoesNothing(bool payProvider)
        {
            // Arrange
            var request = new CDIFailedEvent
            {
                DateTime = DateTimeOffset.UtcNow,
                PayProvider = payProvider
            };

            A.CallTo(() => _mediator.Send(A<GetCKD>._, A<CancellationToken>._))
                .Returns(new Core.Data.Entities.CKD());

            A.CallTo(() => _mediator.Send(A<GetCKDStatuses>._, A<CancellationToken>._))
                .Returns(new List<CKDStatus>
                {
                    new()
                    {
                        CKDStatusCodeId = CKDStatusCode.CKDNotPerformed.CKDStatusCodeId
                    }
                });

            var context = new TestableMessageHandlerContext();

            // Act
            await CreateSubject().Handle(request, context);

            // Assert
            _transactionSupplier.AssertNoTransactionCreated();

            Assert.Empty(context.SentMessages);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Handle_WhenExamDoesNotQualifyForProviderPay_Tests(bool payProvider)
        {
            // Arrange
            const long evaluationId = 1;

            var request = new CDIFailedEvent
            {
                EvaluationId = evaluationId,
                DateTime = DateTimeOffset.UtcNow,
                PayProvider = payProvider
            };

            A.CallTo(() => _mediator.Send(A<GetCKD>._, A<CancellationToken>._))
                .Returns(new Core.Data.Entities.CKD
                {
                    EvaluationId = evaluationId
                });

            A.CallTo(() => _mediator.Send(A<GetCKDStatuses>._, A<CancellationToken>._))
                .Returns(new List<CKDStatus>
                {
                    new()
                    {
                        CKDStatusCodeId = CKDStatusCode.CKDPerformed.CKDStatusCodeId
                    }
                });

            var context = new TestableMessageHandlerContext();

            // Act
            await CreateSubject().Handle(request, context);

            // Assert
            _transactionSupplier.AssertCommit();

            Assert.Equal(2, context.SentMessages.Length);

            var statusCode = payProvider ? CKDStatusCode.CdiFailedWithPayReceived : CKDStatusCode.CdiFailedWithoutPayReceived;

            AssertSent<ProviderPayStatusEvent>(context,
                filter: message => message.StatusCode.CKDStatusCodeId == statusCode.CKDStatusCodeId,
                message =>
                {
                    Assert.Equal(evaluationId, message.EvaluationId);
                    Assert.Equal(request.DateTime, message.StatusDateTime);
                });

            if (!payProvider)
            {
                AssertSent<ProviderPayStatusEvent>(context,
                    filter: message => message.StatusCode.CKDStatusCodeId == CKDStatusCode.ProviderNonPayableEventReceived.CKDStatusCodeId,
                    message =>
                    {
                        Assert.Equal(evaluationId, message.EvaluationId);
                        Assert.Equal(request.DateTime, message.StatusDateTime);
                    });
            }

            if (payProvider)
                A.CallTo(_payableRules).MustHaveHappened();
            else
                A.CallTo(_payableRules).MustNotHaveHappened();
        }

        private static void AssertSent<T>(TestablePipelineContext context, Func<T, bool> filter, Action<T> action)
        {
            foreach (var message in context.SentMessages)
            {
                if (message.Message is not T asType || !filter(asType))
                    continue;

                action(asType);

                return;
            }

            Assert.Fail("Sent message not found");
        }
    }
}
