using AutoMapper;
using FluentAssertions;
using Signify.A1C.Messages.Events;
using Signify.A1C.Svc.Core.Events;
using Signify.A1C.Svc.Core.Maps;
using Signify.A1C.Svc.Core.Sagas;
using System;
using Xunit;

namespace Signify.A1C.Svc.Core.Tests.Maps
{
    public class MappingProfileTests
    {
        [Fact]
        public void Should_Map_A1CPerformed_From_A1C()
        {
            var a1c = new Core.Data.Entities.A1C();
            var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
            var mapper = config.CreateMapper();

            //Action
            var a1cPerformed = mapper.Map<Core.Data.Entities.A1C, A1CPerformedEvent>(a1c);

            //Assert
            a1cPerformed.CorrelationId.Should().NotBeEmpty();
        }

        [Fact]
        public void Should_Map_UpdateInventory()
        {
            var updateInv = new InventoryUpdated
            (
                new Guid(),
                "HBA1C",
                new Events.Result(),
                "000000",
                1,
                -1,
                new DateTime(),
                new DateTime()
            );
            var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
            var mapper = config.CreateMapper();

            //Action
            var req = mapper.Map<InventoryUpdated, InventoryUpdateReceived>(updateInv);

            //Assert
            req.RequestId.Should().Be(updateInv.RequestId);
        }
    }
}
