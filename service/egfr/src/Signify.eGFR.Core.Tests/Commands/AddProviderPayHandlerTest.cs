using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.eGFR.Core.Commands;
using Signify.eGFR.Core.Data.Entities;
using Xunit;

namespace Signify.eGFR.Core.Tests.Commands;

public sealed class AddProviderPayHandlerTest : IDisposable, IAsyncDisposable
{
    private readonly MockDbFixture _dbFixture = new();

    public void Dispose()
    {
        _dbFixture.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _dbFixture.DisposeAsync();
    }

    private CreateProviderPayHandler CreateSubject()
        => new(
            _dbFixture.SharedDbContext, A.Dummy<ILogger<CreateProviderPayHandler>>());

    [Fact]
    public async Task Handle_Insert_ProviderPay_Status()
    {
        //Arrange
        var createProviderPay = A.Fake<AddProviderPay>();
        createProviderPay.ProviderPay = new ProviderPay
        {
            ProviderPayId = 1, ExamId = 1,
            CreatedDateTime = DateTimeOffset.Now,
            PaymentId = Guid.NewGuid().ToString()
        };

        var subject = CreateSubject();

        await subject.Handle(createProviderPay, CancellationToken.None);

        //Assert
        Assert.Equal(1, _dbFixture.SharedDbContext.ProviderPay.Count());
    }
}