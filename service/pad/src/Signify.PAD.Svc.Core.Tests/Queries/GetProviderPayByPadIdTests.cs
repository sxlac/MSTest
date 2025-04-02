using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.PAD.Svc.Core.Queries;
using Signify.PAD.Svc.Core.Tests.Utilities;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Queries;

public class GetProviderPayByPadIdTests : IClassFixture<MockDbFixture>
{
    private readonly GetProviderPayByPadIdHandler _handler;

    public GetProviderPayByPadIdTests(MockDbFixture mockDbFixture)
    {
        var logger = A.Fake<ILogger<GetProviderPayByPadIdHandler>>();
        _handler = new GetProviderPayByPadIdHandler(mockDbFixture.Context, logger);
    }

    [Fact]
    public async Task Handle_WhenEntity_Exists_ReturnsEntity()
    {
        // Arrange
        var query = new GetProviderPayByPadId
        {
            PadId = 1
        };
    
        // Act
        var result = await _handler.Handle(query, CancellationToken.None);
    
        // Assert
        Assert.Equal(query.PadId, result.PADId);
        Assert.Equal("1234ABCD", result.PaymentId);
    }
}