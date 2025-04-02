using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Data.Entities;
using Signify.PAD.Svc.Core.Tests.Fakes.Infrastructure;
using Signify.PAD.Svc.Core.Tests.Utilities;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Commands;

public class CreateOrUpdateRcmBillingTests : IClassFixture<MockDbFixture>
{
    private readonly MockDbFixture _mockDbFixture;
    private readonly CreateOrUpdateRcmBillingHandler _handler;
    private readonly FakeApplicationTime _applicationTime = new();

    public CreateOrUpdateRcmBillingTests(MockDbFixture mockDbFixture)
    {
        _mockDbFixture = mockDbFixture;
        _handler = new CreateOrUpdateRcmBillingHandler(mockDbFixture.Context);
    }

    [Fact]
    public async Task Should_Insert_RCMBilling_Status()
    {
        var rcmBilling = new CreateOrUpdateRcmBilling
        {
            RcmBilling = new PADRCMBilling
            {
                BillId = "1",
                PADId = 1,
                CreatedDateTime = _applicationTime.UtcNow()
            }
        };

        //Act
        var result = await _handler.Handle(rcmBilling, CancellationToken.None);

        //Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Should_Insert_RCMBilling_Count()
    {
        //Arrange
        var rcmBilling = new CreateOrUpdateRcmBilling
        {
            RcmBilling = new PADRCMBilling
            {
                BillId = "1",
                PADId = 1,
                CreatedDateTime = _applicationTime.UtcNow()
            }
        };
        var initialCount = _mockDbFixture.Context.PADRCMBilling.Count();

        //Act
        await _handler.Handle(rcmBilling, CancellationToken.None);

        //Assert
        _mockDbFixture.Context.PADRCMBilling.Count().Should().BeGreaterThan(initialCount);
    }

    [Fact]
    public async Task Should_Update_RCMBilling_When_Entity_AlreadyExist()
    {
        //Arrange
        var rcmBilling = new CreateOrUpdateRcmBilling
        {
            RcmBilling = new PADRCMBilling
            {
                BillId = "1",
                PADId = 1,
                CreatedDateTime = _applicationTime.UtcNow()
                // this adds a new entry with Id = 2 since Id = 1 exist in fixture
            }
        };

        //Act
        await _handler.Handle(rcmBilling, CancellationToken.None);
        var initialCount = _mockDbFixture.Context.PADRCMBilling.Count();
        rcmBilling.RcmBilling.Accepted = true;
        rcmBilling.RcmBilling.AcceptedAt = _applicationTime.UtcNow();
        await _handler.Handle(rcmBilling, CancellationToken.None);

        //Assert
        Assert.Equal(initialCount, _mockDbFixture.Context.PADRCMBilling.Count());
    }
}