using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.CKD.Messages.Events;
using Signify.CKD.Svc.Core.Commands;
using Signify.CKD.Svc.Core.EventHandlers;
using Signify.CKD.Svc.Core.Infrastructure.Observability;
using Signify.CKD.Svc.Core.Tests.Utilities;
using Xunit;

namespace Signify.CKD.Svc.Core.Tests.EventHandlers
{
    public class CKDPerformedHandlerTest : IClassFixture<EntityFixtures>
    {
        private readonly IMapper _mapper;
        private readonly ILogger<CKDPerformedHandler> _logger;
        private readonly IMediator _mediator;
        private readonly IObservabilityService _observabilityService = A.Fake<IObservabilityService>();
        private readonly CKDPerformedHandler _ckdPerformedHandler;
        private readonly EntityFixtures _entityFixtures;
        private readonly TestableMessageHandlerContext _messageHandlerContext;
        public CKDPerformedHandlerTest(EntityFixtures entityFixtures)
        {
            _logger = A.Fake<ILogger<CKDPerformedHandler>>();
            _mapper = A.Fake<IMapper>();
            _mediator = A.Fake<IMediator>();
            _entityFixtures = entityFixtures;
            _ckdPerformedHandler = new CKDPerformedHandler(_logger, _mapper, _mediator, _observabilityService);
            _messageHandlerContext = new TestableMessageHandlerContext();
        }

        [Fact]
        public async Task CKDPerformedHandler_PublishCheck()
        {
            //A.CallTo(() => _mapper.Map<UpdateInventory>(A<CKDPerformed>._)).Returns(_entityFixtures.MockUpdateInventory());
            await _ckdPerformedHandler.Handle(CkdPerformed, _messageHandlerContext);
            //_messageHandlerContext.SentMessages.Length.Should().Be(1);
            A.CallTo(() => _mediator.Send(A<PublishStatusUpdateHandler>._, A<CancellationToken>._)).Returns(Unit.Value);
            A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>._, default)).MustHaveHappenedOnceExactly();
        }

        //[Fact]
        //public async Task CKDPerformedHandler_UpdateInventoryTypeCheck()
        //{
        //    A.CallTo(() => _mapper.Map<UpdateInventory>(A<CKDPerformed>._)).Returns(_entityFixtures.MockUpdateInventory());
        //    await _ckdPerformedHandler.Handle(CkdPerformed, _messageHandlerContext);
        //    _messageHandlerContext.SentMessages[0].Message.Should().BeOfType<UpdateInventory>();
        //}

        //[Fact]
        //public async Task CKDPerformedHandler_UpdateInventoryMap_TimesCalled()
        //{
        //    A.CallTo(() => _mapper.Map<UpdateInventory>(A<CKDPerformed>._)).Returns(_entityFixtures.MockUpdateInventory());
        //    await _ckdPerformedHandler.Handle(CkdPerformed, _messageHandlerContext);
        //    A.CallTo(() => _mapper.Map<UpdateInventory>(A<CKDPerformed>._)).MustHaveHappenedOnceExactly();
        //}

        //[Fact]
        //public async Task CKDPerformedHandler_RecognizeRevenueTypeCheck()
        //{
        //    A.CallTo(() => _mapper.Map<UpdateInventory>(A<CKDPerformed>._)).Returns(_entityFixtures.MockUpdateInventory());
        //    await _ckdPerformedHandler.Handle(CkdPerformed, _messageHandlerContext);
        //    _messageHandlerContext.SentMessages[0].Message.Should().BeOfType(_messageHandlerContext.SentMessages[0].Message.GetType(), "");
        //}

        //[Fact]
        //public async Task CKDPerformedHandler_UpdateInventoryPublishedValueCheck()
        //{
        //    var inventory = _entityFixtures.MockUpdateInventory();
        //    A.CallTo(() => _mapper.Map<UpdateInventory>(A<CKDPerformed>._)).Returns(inventory);
        //    await _ckdPerformedHandler.Handle(CkdPerformed, _messageHandlerContext);
        //    var updateInventory = (UpdateInventory)_messageHandlerContext.SentMessages[0].Message;
        //    updateInventory.ApplicationId.Should().Be(inventory.ApplicationId);
        //}

        private static CKDPerformed CkdPerformed = new CKDPerformed()
        {
            ApplicationId = "Signify.Evaluation.Service",
            AppointmentId = 1000084716,
            ClientId = 14,
            CreatedDateTime = DateTimeOffset.UtcNow,
            DateOfService = DateTime.UtcNow,
            EvaluationId = 324357,
            MemberId = 11990396,
            MemberPlanId = 21074285,
            ProviderId = 42879,
            ReceivedDateTime = DateTime.UtcNow,
            UserName = "vastest1",
            ExpirationDate = DateTime.Now
        };
    }
}
