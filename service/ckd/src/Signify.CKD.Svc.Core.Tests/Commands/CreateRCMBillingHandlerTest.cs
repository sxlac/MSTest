using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Signify.CKD.Svc.Core.Commands;
using Signify.CKD.Svc.Core.Data.Entities;
using Signify.CKD.Svc.Core.Tests.Utilities;
using Xunit;

namespace Signify.CKD.Svc.Core.Tests.Commands;

public class CreateRCMBillingHandlerTest : IClassFixture<EntityFixtures>, IClassFixture<MockDbFixture>
{
    private readonly MockDbFixture _mockDbFixture;
    private readonly CreateRCMBillingHandler _createRcmBillingHandler;
    public CreateRCMBillingHandlerTest(MockDbFixture mockDbFixture)
    {
        _mockDbFixture = mockDbFixture;
        _createRcmBillingHandler = new CreateRCMBillingHandler(mockDbFixture.Context);
    }

    [Fact]
    public async Task Should_Insert_CKDRCMBilling_Status()
    {
        var ckd = new Core.Data.Entities.CKD();
        var rcmBilling = new CreateRCMBilling()
        {
            RcmBilling = new CKDRCMBilling(){BillId = "1",CKD = ckd,CreatedDateTime = DateTimeOffset.Now,Id = 2},
        };

        //Act
        var result = await _createRcmBillingHandler.Handle(rcmBilling, CancellationToken.None);

        //Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Should_Insert_CKDRCMBilling_COunt()
    {
        var ckd = new Core.Data.Entities.CKD();
        var rcmBilling = new CreateRCMBilling()
        {
            RcmBilling = new CKDRCMBilling() { BillId = "1", CKD = ckd, CreatedDateTime = DateTimeOffset.Now, Id = 3 },
        };
        var initialCount = _mockDbFixture.Context.CKDRCMBilling.Count();

        //Act
        await _createRcmBillingHandler.Handle(rcmBilling, CancellationToken.None);

        //Assert
        _mockDbFixture.Context.CKDRCMBilling.Count().Should().BeGreaterThan(initialCount);
    }
}