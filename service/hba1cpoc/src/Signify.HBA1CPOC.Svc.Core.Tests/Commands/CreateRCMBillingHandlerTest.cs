using FluentAssertions;
using Signify.HBA1CPOC.Svc.Core.Commands;
using Signify.HBA1CPOC.Svc.Core.Data.Entities;
using Signify.HBA1CPOC.Svc.Core.Tests.Utilities;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.HBA1CPOC.Svc.Core.Tests.Commands;

public class CreateRCMBillingHandlerTest : IClassFixture<MockDbFixture>
{
    private readonly MockDbFixture _mockDbFixture;
    private readonly CreateRCMBillingHandler _createRcmBillingHandler;

    public CreateRCMBillingHandlerTest(MockDbFixture mockDbFixture)
    {
        _mockDbFixture = mockDbFixture;
        _createRcmBillingHandler = new CreateRCMBillingHandler(mockDbFixture.Context);
    }

    [Fact]
    public async Task Should_Insert_Billing_Status()
    {
        var entity = new Core.Data.Entities.HBA1CPOC();
        var rcmBilling = new CreateOrUpdateRCMBilling
        {
            RcmBilling = new HBA1CPOCRCMBilling { Id = 0, BillId = Guid.NewGuid().ToString(), HBA1CPOCId = entity.HBA1CPOCId, CreatedDateTime = DateTimeOffset.Now }
        };
        int initialCount = _mockDbFixture.Context.HBA1CPOCRCMBilling.Count();

        //Act
        var result = await _createRcmBillingHandler.Handle(rcmBilling, CancellationToken.None);

        //Assert
        result.Should().NotBeNull();
        _mockDbFixture.Context.HBA1CPOCRCMBilling.Count().Should().BeGreaterThan(initialCount);
    }
}