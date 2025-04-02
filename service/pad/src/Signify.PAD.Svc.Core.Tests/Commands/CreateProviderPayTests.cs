using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Data.Entities;
using Signify.PAD.Svc.Core.Tests.Utilities;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Commands;

public class CreateProviderPayTests : IClassFixture<MockDbFixture>
{
    private readonly MockDbFixture _mockDbFixture;
    private readonly CreateProviderPayHandler _handler;

    public CreateProviderPayTests(MockDbFixture mockDbFixture)
    {
        _mockDbFixture = mockDbFixture;
        _handler = new CreateProviderPayHandler(_mockDbFixture.Context, A.Dummy<ILogger<CreateProviderPayHandler>>());
    }

    [Fact]
    public async Task Handle_Insert_ProviderPay_Status()
    {
        //Arrange
        var createProviderPay = A.Fake<CreateProviderPay>();
        createProviderPay.ProviderPay = new ProviderPay
        {
            ProviderPayId = 100, PADId = 1, 
            CreatedDateTime = DateTimeOffset.Now, 
            PaymentId = Guid.NewGuid().ToString()
        };
        
        //Act
        var result = await _handler.Handle(createProviderPay, CancellationToken.None);
        
        //Assert
        Assert.Equal(Unit.Value, result);
    }

    [Fact]
    public async Task Handle_Insert_ProviderPay_VerifyCount()
    {
        //Arrange
        var providerPay = new CreateProviderPay
        {
            ProviderPay = new ProviderPay
            {
                ProviderPayId = 2, PADId = 1, 
                CreatedDateTime = DateTimeOffset.Now,
                PaymentId = Guid.NewGuid().ToString()
            },
        };
        var initialCount = _mockDbFixture.Context.ProviderPay.Count();

        //Act
        await _handler.Handle(providerPay, CancellationToken.None);

        //Assert
        Assert.True(_mockDbFixture.Context.ProviderPay.Count() > initialCount);
    }
}