using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.HBA1CPOC.Svc.Core.Commands;
using Signify.HBA1CPOC.Svc.Core.Data.Entities;
using Signify.HBA1CPOC.Svc.Core.Tests.Utilities;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.HBA1CPOC.Svc.Core.Tests.Commands;

public class CreateProviderPayHandlerTests : IClassFixture<MockDbFixture>
{
    private readonly MockDbFixture _mockDbFixture;
    private readonly CreateProviderPayHandler _createProviderPayHandler;

    public CreateProviderPayHandlerTests(MockDbFixture mockDbFixture)
    {
        _mockDbFixture = mockDbFixture;
        _createProviderPayHandler = new CreateProviderPayHandler(_mockDbFixture.Context, A.Dummy<ILogger<CreateProviderPayHandler>>());
    }

    [Fact]
    public async Task Handle_Insert_ProviderPay_Status()
    {
        //Arrange
        var createProviderPay = A.Fake<CreateProviderPay>();
        createProviderPay.ProviderPay = new ProviderPay
        {
            ProviderPayId = 1, 
            HBA1CPOCId = 1, 
            CreatedDateTime = DateTimeOffset.Now, 
            PaymentId = Guid.NewGuid().ToString()
        };
        
        //Act
        var result = await _createProviderPayHandler.Handle(createProviderPay, CancellationToken.None);
        
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
                ProviderPayId = 2, 
                HBA1CPOCId = 1, 
                CreatedDateTime = DateTimeOffset.Now,
                PaymentId = Guid.NewGuid().ToString()
            },
        };
        var initialCount = _mockDbFixture.Context.ProviderPay.Count();

        //Act
        await _createProviderPayHandler.Handle(providerPay, CancellationToken.None);

        //Assert
        Assert.True(_mockDbFixture.Context.ProviderPay.Count() > initialCount);
    }
}