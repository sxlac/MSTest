using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.CKD.Messages.Events;
using Signify.CKD.Svc.Core.EventHandlers;
using Signify.CKD.Svc.Core.Events;
using Signify.CKD.Svc.Core.Filters;
using Signify.CKD.Svc.Core.Infrastructure.Observability;
using Xunit;

namespace Signify.CKD.Svc.Core.Tests.EventHandlers
{
    public class EvaluationFinalizedHandlerTest
    {
        private readonly IProductFilter _productFilter = A.Fake<IProductFilter>();
        private readonly IMapper _mapper;
        private readonly IObservabilityService _observabilityService = A.Fake<IObservabilityService>();
        private readonly EvaluationFinalizedHandler _evaluationFinalizedHandler;
        private readonly TestableEndpointInstance _messageSessionInstance;

        public EvaluationFinalizedHandlerTest()
        {
            _messageSessionInstance = new TestableEndpointInstance();
            _mapper = A.Fake<IMapper>();
            _evaluationFinalizedHandler = new EvaluationFinalizedHandler(A.Dummy<ILogger<EvaluationFinalizedHandler>>(), _productFilter, _messageSessionInstance, _mapper, _observabilityService);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Handle_WithProductCodes_Tests(bool shouldEnqueue)
        {
            var message = new EvaluationFinalizedEvent();

            A.CallTo(() => _productFilter.ShouldProcess(A<IEnumerable<Product>>._))
                .Returns(shouldEnqueue);

            await _evaluationFinalizedHandler.Handle(message, default);

            if (shouldEnqueue)
                A.CallTo(() => _mapper.Map<EvalReceived>(A<EvaluationFinalizedEvent>._)).MustHaveHappened();

            _messageSessionInstance.SentMessages.Length.Should().Be(shouldEnqueue ? 1 : 0);
        }
    }
}
