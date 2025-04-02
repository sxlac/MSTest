using System;
using System.Threading;
using System.Threading.Tasks;
using Signify.DEE.Svc.Core.Messages.Queries;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Messages.Queries;

public class GetRcmBillingByBillIdTests(MockDbFixture mockDbFixture) : IClassFixture<MockDbFixture>
{
    private readonly GetRcmBillingByBillIdHandler _handler = new(mockDbFixture.FakeDatabaseContext);

    [Theory]
    [InlineData("0C58DF47-B8B7-48FF-BC4F-CC532B6DF653")]
    [InlineData("0c58df47-b8b7-48ff-bc4f-cc532b6df653")]
    public async Task Handle_WhenEntity_Exists_ReturnsEntity(string billId)
    {
        // Arrange
        var query = new GetRcmBillingByBillId(billId);
    
        // Act
        var result = await _handler.Handle(query, CancellationToken.None);
    
        // Assert
        Assert.NotNull(result);
        var isMatch = result.BillId.Equals(query.BillId, StringComparison.OrdinalIgnoreCase);
        Assert.True(isMatch);
    }
    
    [Theory]
    [InlineData("19E1B54A-E9F8-4599-B4B6-EED91016C14A")]
    public async Task Handle_WhenEntity_DoesNot_Exists_ReturnsNull(string billId)
    {
        // Arrange
        var query = new GetRcmBillingByBillId(billId);
    
        // Act
        var result = await _handler.Handle(query, CancellationToken.None);
    
        // Assert
        Assert.Null(result);
    }
}