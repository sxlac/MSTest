using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.Spirometry.Core.Commands;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Data;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Commands;

public sealed class AddProviderPayHandlerTests : IDisposable, IAsyncDisposable
{
    private readonly MockDbFixture _dbFixture = new();
    private readonly ILogger<AddProviderPayHandler> _logger = A.Fake<ILogger<AddProviderPayHandler>>();

    public void Dispose()
    {
        _dbFixture.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _dbFixture.DisposeAsync();
    }

    private AddProviderPayHandler CreateSubject(SpirometryDataContext context)
        => new(_logger, context);

    [Fact]
    public async Task Handle_Add_NewEntity()
    {
        var providerPay = A.Fake<ProviderPay>();
        providerPay.PaymentId = Guid.NewGuid().ToString();
        var request = new AddProviderPay(providerPay);
        var subject = CreateSubject(_dbFixture.SharedDbContext);

        var actual = await subject.Handle(request, default);

        Assert.NotNull(actual);
        Assert.Equal(providerPay.PaymentId, actual.PaymentId);
    }

    [Fact]
    public async Task Handle_Throws_Exception()
    {
        var providerPay = A.Fake<ProviderPay>();
        providerPay.PaymentId = Guid.NewGuid().ToString();
        var request = new AddProviderPay(providerPay);
        var context = A.Fake<SpirometryDataContext>();
        var subject = CreateSubject(context);
        A.CallTo(() => context.SaveChangesAsync(A<CancellationToken>._)).Throws<Exception>();

        await Assert.ThrowsAnyAsync<Exception>(async () => await subject.Handle(request, default));

        A.CallTo(_logger).Where(call => call.Method.Name == "Log" && call.GetArgument<LogLevel>(0) == LogLevel.Error).MustHaveHappenedOnceExactly();
    }
}