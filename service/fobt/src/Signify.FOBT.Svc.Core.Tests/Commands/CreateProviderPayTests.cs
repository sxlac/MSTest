using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.FOBT.Svc.Core.Commands;
using Signify.FOBT.Svc.Core.Data.Entities;
using Signify.FOBT.Svc.Core.Tests.Utilities;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.Commands;

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
            Id = 100,
            FOBTId = 1, 
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
                Id = 101,
                FOBTId = 2, 
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