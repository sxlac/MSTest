using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.HBA1CPOC.Messages.Events;
using Signify.HBA1CPOC.Sagas;
using Signify.HBA1CPOC.Svc.Core.EventHandlers;
using Signify.HBA1CPOC.Svc.Core.Tests.Mocks.StaticEntity;
using Signify.HBA1CPOC.Svc.Core.Tests.Utilities;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;
using Result = Signify.HBA1CPOC.Messages.Events.Result;

namespace Signify.HBA1CPOC.Svc.Core.Tests.EventHandlers.Nsb;

public class InventoryUpdatedHandlerTests
{
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly InventoryUpdatedHandler _inventoryUpdatedHandler;
    private readonly TestableMessageSession _session = new();

    public InventoryUpdatedHandlerTests()
    {
        _inventoryUpdatedHandler = new InventoryUpdatedHandler(A.Dummy<ILogger<InventoryUpdatedHandler>>(), _session, _mapper);
    }

    [Fact]
    public async Task Should_Publish_UpdateReceived_Event_For_HBA1CPOC()
    {
        //Act
        A.CallTo(() => _mapper.Map<InventoryUpdateReceived>(A<InventoryUpdated>._)).Returns(EntityFixtures.MockInventoryUpdateReceived());
        await _inventoryUpdatedHandler.Handle(StaticMockEntities.InventoryUpdated, CancellationToken.None);

        //Assert
        _session.PublishedMessages.Length.Should().Be(1);
    }

    [Fact]
    public async Task Should_Publish_OfType_InventoryUpdateReceived()
    {
        //Act
        A.CallTo(() => _mapper.Map<InventoryUpdateReceived>(A<InventoryUpdated>._)).Returns(EntityFixtures.MockInventoryUpdateReceived());
        await _inventoryUpdatedHandler.Handle(StaticMockEntities.InventoryUpdated, CancellationToken.None);

        //Assert
        _session.PublishedMessages[0].Message.Should().BeOfType<InventoryUpdateReceived>();
    }

    [Fact]
    public async Task Mapper_Should_Be_Called_Once()
    {
        //Act
        A.CallTo(() => _mapper.Map<InventoryUpdateReceived>(A<InventoryUpdated>._)).Returns(EntityFixtures.MockInventoryUpdateReceived());
        await _inventoryUpdatedHandler.Handle(StaticMockEntities.InventoryUpdated, CancellationToken.None);
        //Assert
        A.CallTo(() => _mapper.Map<InventoryUpdateReceived>(A<InventoryUpdated>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Should_Not_Publish_UpdateReceived_Event_For_Non_HBA1CPOC()
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
        _session.PublishedMessages.Length.Should().Be(0);
    }
}