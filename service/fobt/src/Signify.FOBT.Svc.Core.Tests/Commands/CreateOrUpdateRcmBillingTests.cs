using Signify.FOBT.Svc.Core.Commands;
using Signify.FOBT.Svc.Core.Data.Entities;
using Signify.FOBT.Svc.Core.Tests.Utilities;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.Commands;

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
    public async Task Should_Insert_FobtBilling_When_NoDataExist()
    {
        // Arrange
        var rcmBilling = new CreateOrUpdateRcmBilling
        {
            RcmBilling = new FOBTBilling
            {
                BillId = Guid.NewGuid().ToString(),
                FOBTId = 1,
                CreatedDateTime = _applicationTime.UtcNow()
            }
        };
        var initialCount = _mockDbFixture.Context.FOBTBilling.Count();
        // Act
        await _handler.Handle(rcmBilling, CancellationToken.None);

        // Assert
        Assert.Equal(initialCount + 1, _mockDbFixture.Context.FOBTBilling.Count());
    }

    [Fact]
    public async Task Should_Update_FobtBilling_When_DataExist()
    {
        // Arrange
        var rcmBilling = new CreateOrUpdateRcmBilling
        {
            RcmBilling = new FOBTBilling
            {
                BillId = Guid.NewGuid().ToString(),
                FOBTId = 1,
                CreatedDateTime = _applicationTime.UtcNow()
                // Id will be 3 for the new entry
            }
        };

        // Act
        await _handler.Handle(rcmBilling, CancellationToken.None);
        var latestId = _mockDbFixture.Context.FOBTBilling.Select(e => e.Id).OrderByDescending(e => e).First();
        var initialCount = _mockDbFixture.Context.FOBTBilling.Count();
        rcmBilling.RcmBilling!.Accepted = true;
        rcmBilling.RcmBilling!.AcceptedAt = _applicationTime.UtcNow().AddDays(1);
        await _handler.Handle(rcmBilling, CancellationToken.None);

        // Assert
        Assert.Equal(initialCount, _mockDbFixture.Context.FOBTBilling.Count());
        Assert.True(_mockDbFixture.Context.FOBTBilling.FirstOrDefault(e => e.Id == latestId)!.Accepted);
        Assert.Equal(_applicationTime.UtcNow().AddDays(1), _mockDbFixture.Context.FOBTBilling.FirstOrDefault(e => e.Id == latestId)!.AcceptedAt);
    }
}