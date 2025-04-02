using FluentAssertions;
using Signify.HBA1CPOC.Svc.Core.Queries;
using Signify.HBA1CPOC.Svc.Core.Tests.Utilities;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.HBA1CPOC.Svc.Core.Tests.Queries;

public class GetRcmBillingByBillIdTests : IClassFixture<MockDbFixture>
{
    private readonly GetRcmBillingByBillIdHandler _handler;

    public GetRcmBillingByBillIdTests(MockDbFixture mockDbFixture)
    {
        _handler = new GetRcmBillingByBillIdHandler(mockDbFixture.Context);
    }
    
    [Theory]
    [InlineData("177141de-52f1-5514-8d68-ee0c3c5ee680")]
    [InlineData("29a645df-2419-5468-9132-b703ee84b00b")]
    [InlineData("d595b8c8-6864-5343-a87e-b765996962a3")]
    public async Task GetRcmBillingByBillIdHandler_RequestCorrectHbA1cPocId_SuccessfulResponse(string billId)
    {
        // Arrange
        var query = new GetRcmBillingByBillId() { BillId = billId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(result.BillId, query.BillId);
    }

    [Theory]
    [InlineData("277141de-52f1-5514-8d68-ee0c3c5ee680")]
    [InlineData("39a645df-2419-5468-9132-b703ee84b00b")]
    [InlineData("e595b8c8-6864-5343-a87e-b765996962a3")]
    public async Task GetRcmBillingByBillIdHandler_RequestIncorrectHbA1cPocId_NullResponse(string billId)
    {
        // Arrange
        var query = new GetRcmBillingByBillId() { BillId = billId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}
