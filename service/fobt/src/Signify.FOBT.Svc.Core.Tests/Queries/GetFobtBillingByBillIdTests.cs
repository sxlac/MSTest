using Signify.FOBT.Svc.Core.Queries;
using Signify.FOBT.Svc.Core.Tests.Utilities;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.Queries;

public class GetFobtBillingByBillIdTests : IClassFixture<MockDbFixture>
{
    private readonly GetFobtBillingByBillIdHandler _handler;

    public GetFobtBillingByBillIdTests(MockDbFixture mockDbFixture)
    {
        _handler = new GetFobtBillingByBillIdHandler(mockDbFixture.Context);
    }
    
    [Theory]
    [InlineData("C41D95C7-5F89-4BD6-8EFD-A26E3A3B52CA")]
    [InlineData("c41d95c7-5f89-4bd6-8efd-a26e3a3b52ca")]
    [InlineData("8EEE5D8A-7531-47EE-922E-BB1FEED0089C")]
    public async Task Handle_WhenEntity_Exists_ReturnsEntity(string billId)
    {
        // Arrange
        var query = new GetFobtBillingByBillId(billId);
    
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
        var query = new GetFobtBillingByBillId(billId);
    
        // Act
        var result = await _handler.Handle(query, CancellationToken.None);
    
        // Assert
        Assert.Null(result);
    }
}