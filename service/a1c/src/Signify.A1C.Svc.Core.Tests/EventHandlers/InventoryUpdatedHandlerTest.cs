using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.A1C.Svc.Core.EventHandlers;
using Signify.A1C.Svc.Core.Events;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.A1C.Svc.Core.Tests.EventHandlers
{
    public class InventoryUpdatedHandlerTest
    {
        private readonly ILogger<InventoryUpdatedHandler> _logger;
        private readonly IMapper _mapper;
        private readonly InventoryUpdatedHandler _inventoryUpdatedHandler;
        private readonly TestableEndpointInstance _endpointInstance;

        public InventoryUpdatedHandlerTest()
        {
            _logger = A.Fake<ILogger<InventoryUpdatedHandler>>();
            _mapper = A.Fake<IMapper>();
            _endpointInstance = new TestableEndpointInstance();
            _inventoryUpdatedHandler = new InventoryUpdatedHandler(_logger, _endpointInstance, _mapper);
        }

        [Fact]
        public async Task Should_Publish_UpdateReceived_Event_For_A1C()
        {
            InventoryUpdated evt = new InventoryUpdated
            (
                new Guid(),
                "HBA1C",
                new Result(),
                "000000",
                1,
                -1,
                new DateTime(),
                new DateTime()
            );

            //Act
            await _inventoryUpdatedHandler.Handle(evt, CancellationToken.None);

            //Assert
            _endpointInstance.PublishedMessages.Length.Should().Be(1);
        }

        [Fact]
        public async Task Should_Not_Publish_UpdateReceived_Event_For_Non_A1C()
        {
            InventoryUpdated evt = new InventoryUpdated
            (
                new Guid(),
                "A1C",
                new Result(),
                "000000",
                1,
                -1,
                new DateTime(),
                new DateTime()
            );

            //Act
            await _inventoryUpdatedHandler.Handle(evt, CancellationToken.None);

            //Assert
            _endpointInstance.PublishedMessages.Length.Should().Be(0);
        }
    }
}
