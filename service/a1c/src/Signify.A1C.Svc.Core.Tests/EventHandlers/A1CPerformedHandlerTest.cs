using System.Threading.Tasks;
using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NServiceBus.Testing;
using Signify.A1C.Messages.Events;
using Signify.A1C.Svc.Core.ApiClient.Requests;
using Signify.A1C.Svc.Core.EventHandlers;
using Signify.A1C.Svc.Core.Tests.Utilities;
using Xunit;

namespace Signify.A1C.Svc.Core.Tests.EventHandlers
{
    public class A1CPerformedHandlerTest : IClassFixture<EntityFixtures>
    {
        private readonly A1CPerformedHandler _a1CPerformedHandler;
        private readonly TestableMessageHandlerContext _context;
        private readonly IMapper _mapper;

        public A1CPerformedHandlerTest(EntityFixtures entityFixtures)
        {
            var logger = A.Fake<ILogger<A1CPerformedHandler>>();
            _mapper = A.Fake<IMapper>();
            _a1CPerformedHandler = new A1CPerformedHandler(logger, _mapper);
            _context = new TestableMessageHandlerContext();
        }

        [Fact]
        public async Task Should_Publish_Message()
        {
            A.CallTo(() => _mapper.Map<UpdateInventoryRequest>(A<A1CPerformedEvent>._)).Returns(GetInventoryRequest());
            await _a1CPerformedHandler.Handle(GetA1CPerformedEvent(), _context);
            _context.SentMessages.Length.Should().Be(2);
        }

        [Fact]
        public async Task Should_Publish_UpdateInventoryRequest()
        {
            A.CallTo(() => _mapper.Map<UpdateInventoryRequest>(A<A1CPerformedEvent>._)).Returns(GetInventoryRequest());
            await _a1CPerformedHandler.Handle(GetA1CPerformedEvent(), _context);
            _context.SentMessages[0].Message.Should().BeOfType<UpdateInventoryRequest>();
        }
        [Fact]
        public async Task Should_Call_Mapping()
        {
            A.CallTo(() => _mapper.Map<UpdateInventoryRequest>(A<A1CPerformedEvent>._)).Returns(GetInventoryRequest());
            await _a1CPerformedHandler.Handle(GetA1CPerformedEvent(), _context);
            A.CallTo(() => _mapper.Map<UpdateInventoryRequest>(A<A1CPerformedEvent>._)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task A1CPerformedHandler_UpdateInventoryPublishedValueCheck()
        {
            var invRequest = GetInventoryRequest();
            A.CallTo(() => _mapper.Map<UpdateInventoryRequest>(A<A1CPerformedEvent>._)).Returns(invRequest);
            await _a1CPerformedHandler.Handle(GetA1CPerformedEvent(), _context);
            var updateInventory = (UpdateInventoryRequest)_context.SentMessages[0].Message;
            updateInventory.EvaluationId.Should().Be(invRequest.EvaluationId);
        }

        private static UpdateInventoryRequest GetInventoryRequest()
        {
            const string jsonResponse = ContentHelper.InventoryUpdateRequest;
            return JsonConvert.DeserializeObject<UpdateInventoryRequest>(jsonResponse);
        }

        private static A1CPerformedEvent GetA1CPerformedEvent()
        {
            return new A1CPerformedEvent
            {
                A1CId = 500,
                Barcode = "8A8C"
            };
        }
    }
}
