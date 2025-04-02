using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.CKD.Messages.Events;
using Signify.CKD.Svc.Core.EventHandlers;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.CKD.Svc.Core.Tests.EventHandlers
{
    public class InventoryUpdatedHandlerTests 
    {
        private readonly InventoryUpdatedHandler _inventoryUpdatedHandler;
        private readonly TestableEndpointInstance _messageSessionInstance;

        public InventoryUpdatedHandlerTests()
        {
            var logger = A.Fake<ILogger<InventoryUpdatedHandler>>();
            var mapper = A.Fake<IMapper>();
            _messageSessionInstance = new TestableEndpointInstance();
            _inventoryUpdatedHandler =
                new InventoryUpdatedHandler(logger, _messageSessionInstance, mapper);
        }

        [Fact]
        public async Task Handle_WithValidInventoryUpdated_ShouldPublishEvent()
        {
            var evt = new InventoryUpdated
            {
                RequestId = Guid.NewGuid(),
                ItemNumber = "CKD",
                Result = new Result(),
                SerialNumber = "000000",
                Quantity = 1,
                ProviderId = -1,
                DateUpdated = new DateTime(),
                ExpirationDate = new DateTime()
            };

            //Act
            await _inventoryUpdatedHandler.Handle(evt, CancellationToken.None);

            //Assert
            _messageSessionInstance.SentMessages.Length.Should().Be(1);
        }

        [Fact]
        public async Task Handle_WithInvalidInventoryUpdated_ShouldNotPublishEvent()
        {
            var evt = new InventoryUpdated
            {
                RequestId = Guid.NewGuid(),
                ItemNumber = "A1C",
                Result = new Result(),
                SerialNumber = "000000",
                Quantity = 1,
                ProviderId = -1,
                DateUpdated = new DateTime(),
                ExpirationDate = new DateTime()
            };

            //Act
            await _inventoryUpdatedHandler.Handle(evt, CancellationToken.None);

            //Assert
            _messageSessionInstance.PublishedMessages.Length.Should().Be(0);
        }
    }
}
