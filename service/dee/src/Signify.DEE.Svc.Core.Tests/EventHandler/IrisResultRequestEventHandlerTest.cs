using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.DEE.Svc.Core.EventHandlers;
using Signify.DEE.Svc.Core.Events;
using Signify.DEE.Svc.Core.Messages.Commands;
using Signify.DEE.Svc.Core.Messages.Models;
using Signify.DEE.Svc.Core.Messages.Queries;
using Signify.DEE.Svc.Core.Tests.Utilities;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.EventHandler
{
    public class IrisResultRequestEventHandlerTest : IClassFixture<EntityFixtures>, IClassFixture<MockDbFixture>
    {
        private readonly ILogger<IrisResultRequestEventHandler> _logger;
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;

        private readonly IrisResultRequestEventHandler _irisResultRequestEventHandler;
        private readonly TestableEndpointInstance _endpointInstance;
        private readonly EntityFixtures _entityFixtures;
        public IrisResultRequestEventHandlerTest(EntityFixtures entityFixtures, MockDbFixture mockDbFixture)
        {
            _endpointInstance = new TestableEndpointInstance();
            _logger = A.Fake<ILogger<IrisResultRequestEventHandler>>();
            _mediator = A.Fake<IMediator>();
            _mapper = A.Fake<IMapper>();
            _entityFixtures = entityFixtures;
            _irisResultRequestEventHandler = new IrisResultRequestEventHandler(_logger, _mediator); 
        }

        [Fact]
        public async Task IrisResultRequestEventHandler_GradableImage()
        {
            IrisResultRequestEvent reqevent = new IrisResultRequestEvent()
            {
                CreatedDateTime = DateTime.Now,
                DeeExamId = 916936,
                ExamId = 2379,
                RequestId = new Guid("9e927a6d-d4a6-408f-adfc-84038c6786fd")
            };
            var messageHandlerContext = new TestableMessageHandlerContext();
            var evaluationDocModel = new EvaluationDocumentModel() { CreatedDateTime = DateTime.Now, DocumentType = "pdf", EvaluationId = 331339 };
            var examResultModel = new ExamResultModel() { ExamId = 1376, GradableImage = true };

            var _examModel = new ExamModel() { EvaluationId = 331339, ExamId = 2379 };
            byte[] sampleImage = new byte[] { 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01 };

            //Setup
            A.CallTo(() => _mediator.Send(A<GetExamRecord>._, A<CancellationToken>._)).Returns(_examModel);
            A.CallTo(() => _mediator.Send(A<GetIrisResultPdf>._, A<CancellationToken>._)).Returns(sampleImage);
            A.CallTo(() => _mediator.Send(A<StatusUpdate>._, A<CancellationToken>._)).Returns(true);
            A.CallTo(() => _mediator.Send(A<GetIrisResultData>._, A<CancellationToken>._)).Returns(examResultModel);

            await _irisResultRequestEventHandler.Handle(reqevent, messageHandlerContext);
            messageHandlerContext.PublishedMessages.Length.Should().Be(0);
        }

        [Fact]
        public async Task IrisResultRequestEventHandler_NotGradableImage()
        {
            IrisResultRequestEvent reqevent = new IrisResultRequestEvent()
            {
                CreatedDateTime = DateTime.Now,
                DeeExamId = 916936,
                ExamId = 2379,
                RequestId = new Guid("9e927a6d-d4a6-408f-adfc-84038c6786fd")
            };
            var messageHandlerContext = new TestableMessageHandlerContext();
            var evaluationDocModel = new EvaluationDocumentModel() { CreatedDateTime = DateTime.Now, DocumentType = "pdf", EvaluationId = 331339 };
            var examResultModel = new ExamResultModel() { ExamId = 1376, GradableImage = false };

            var _examModel = new ExamModel() { EvaluationId = 331339, ExamId = 2379 };
            byte[] sampleImage = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

            //Setup
            A.CallTo(() => _mediator.Send(A<GetExamRecord>._, A<CancellationToken>._)).Returns(_examModel);
            A.CallTo(() => _mediator.Send(A<GetIrisResultPdf>._, A<CancellationToken>._)).Returns(sampleImage);
            A.CallTo(() => _mediator.Send(A<StatusUpdate>._, A<CancellationToken>._)).Returns(true);
            A.CallTo(() => _mediator.Send(A<GetIrisResultData>._, A<CancellationToken>._)).Returns(examResultModel);

            await _irisResultRequestEventHandler.Handle(reqevent, messageHandlerContext);
            messageHandlerContext.PublishedMessages.Length.Should().Be(0);
        }
    }
}
