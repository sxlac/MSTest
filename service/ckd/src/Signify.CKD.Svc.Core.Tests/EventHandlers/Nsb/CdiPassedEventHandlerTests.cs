using System;
using System.Collections.Generic;
using System.Linq;
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
    public class CdiPassedEventHandlerTests
    {
        private readonly IMediator _mediator = A.Fake<IMediator>();
        private readonly IMapper _mapper = A.Fake<IMapper>();
        private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();
        private readonly FakeTransactionSupplier _transactionSupplier = new();
        private readonly IPayableRules _payableRules = A.Fake<IPayableRules>();

        private CdiPassedEventHandler CreateSubject()
            => new(A.Dummy<ILogger<CdiPassedEventHandler>>(), _mediator, _mapper, _publishObservability, _transactionSupplier, _payableRules);

        [Fact]
        public async Task Handle_WhenNotPerformed_DoesNothing()
        {
            // Arrange
            var request = new CDIPassedEvent
            {
                DateTime = DateTimeOffset.UtcNow
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

        [Fact]
        public async Task Handle_WhenPerformed_PublishesCdiPassedReceived()
        {
            // Arrange
            const long evaluationId = 1;

            var message = new CDIPassedEvent
            {
                EvaluationId = evaluationId,
                DateTime = DateTimeOffset.UtcNow
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
            await CreateSubject().Handle(message, context);

            // Assert
            _transactionSupplier.AssertCommit();

            var cdiPassedEvent = context.SentMessages.SingleOrDefault(each =>
                    each.Message is ProviderPayStatusEvent e &&
                    e.StatusCode.CKDStatusCodeId == CKDStatusCode.CdiPassedReceived.CKDStatusCodeId)
                ?.Message<ProviderPayStatusEvent>();

            Assert.NotNull(cdiPassedEvent);
            Assert.Equal(evaluationId, cdiPassedEvent.EvaluationId);
            Assert.Equal(message.DateTime, cdiPassedEvent.StatusDateTime);
        }
    }
}
