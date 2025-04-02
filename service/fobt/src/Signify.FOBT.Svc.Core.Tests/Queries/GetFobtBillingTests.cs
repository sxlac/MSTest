using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Signify.FOBT.Svc.Core.Constants;
using Signify.FOBT.Svc.Core.Queries;
using Signify.FOBT.Svc.Core.Tests.Utilities;
using System.Threading.Tasks;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.Queries;

public class GetFobtBillingTests : IClassFixture<MockDbFixture>
{
    private readonly GetFobtBillingHandler _handler;

    public GetFobtBillingTests(MockDbFixture mockDbFixture)
    {
        var logger = A.Fake<ILogger<GetFobtBillingHandler>>();

        _handler = new GetFobtBillingHandler(mockDbFixture.Context, logger);
    }

    [Fact]
    public async Task GetFobtBillingHandler_ResultFromLookupNotFound_ReturnNullValue()
    {
        // Arrange
        var request = new GetFobtBilling(12345, ApplicationConstants.BILLING_PRODUCT_CODE_LEFT);

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetFobtBillingHandler_ResultFromLookupFound_ReturnValue()
    {
        // Arrange
        var request = new GetFobtBilling(1, ApplicationConstants.BILLING_PRODUCT_CODE_LEFT);

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
    }
}