using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.CKD.Svc.Core.Commands;
using Signify.CKD.Svc.Core.Data.Entities;
using Signify.CKD.Svc.Core.Tests.Utilities;
using Xunit;

namespace Signify.CKD.Svc.Core.Tests.Commands;

public class CreateProviderPayHandlerTest : IClassFixture<MockDbFixture>
{
    private readonly MockDbFixture _mockDbFixture;
    private readonly CreateProviderPayHandler _createProviderPayHandler;

    public CreateProviderPayHandlerTest(MockDbFixture mockDbFixture)
    {
        _mockDbFixture = mockDbFixture;
        _createProviderPayHandler = new CreateProviderPayHandler(_mockDbFixture.Context, A.Dummy<ILogger<CreateProviderPayHandler>>());
    }

    [Fact]
    public async Task Handle_Insert_ProviderPay_Status()
    {
        var ckd = A.Fake<Core.Data.Entities.CKD>();
        var createProviderPay = A.Fake<CreateProviderPay>();
        createProviderPay.ProviderPay = new ProviderPay
            { ProviderPayId = 1, CKD = ckd, CreatedDateTime = DateTimeOffset.Now, PaymentId = Guid.NewGuid().ToString() };

        var result = await _createProviderPayHandler.Handle(createProviderPay, CancellationToken.None);

        Assert.Equal(Unit.Value, result);
    }

    [Fact]
    public async Task Handle_Insert_ProviderPay_VerifyCount()
    {
        var ckd = A.Fake<Core.Data.Entities.CKD>();
        var providerPay = new CreateProviderPay()
        {
            ProviderPay = new ProviderPay { ProviderPayId = 2, CKD = ckd, CreatedDateTime = DateTimeOffset.Now, PaymentId = Guid.NewGuid().ToString() },
        };
        var initialCount = _mockDbFixture.Context.ProviderPay.Count();

        await _createProviderPayHandler.Handle(providerPay, CancellationToken.None);

        Assert.True(_mockDbFixture.Context.ProviderPay.Count() > initialCount);
    }
}