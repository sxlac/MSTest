using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Signify.DEE.Svc.Core.Commands;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Commands;

public class CreateProviderPayTests
{
    private readonly DataContext _context;
    private readonly CreateProviderPayHandler _createProviderPayHandler;
    private readonly FakeApplicationTime _applicationTime = new();

    public CreateProviderPayTests()
    {
        var options = new DbContextOptionsBuilder<DataContext>().UseInMemoryDatabase(databaseName: "DEE_CREATE_PROVIDER_PAY_TEST").Options;
        _context = new DataContext(options);
        _createProviderPayHandler = new CreateProviderPayHandler(_context, A.Dummy<ILogger<CreateProviderPayHandler>>());
    }

    [Fact]
    public async Task Handle_Insert_ProviderPay_Status()
    {
        var createProviderPay = new CreateProviderPay
        {
            ProviderPay = new ProviderPay { Id = 1, CreatedDateTime = _applicationTime.LocalNow(), PaymentId = Guid.NewGuid().ToString() },
        };
        var result = await _createProviderPayHandler.Handle(createProviderPay, CancellationToken.None);

        Assert.Equal(Unit.Value, result);
    }

    [Fact]
    public async Task Handle_Insert_ProviderPay_VerifyCount()
    {
        var providerPay = new CreateProviderPay
        {
            ProviderPay = new ProviderPay { Id = 2, CreatedDateTime = _applicationTime.LocalNow(), PaymentId = Guid.NewGuid().ToString() },
        };
        var initialCount = _context.ProviderPays.Count();

        await _createProviderPayHandler.Handle(providerPay, CancellationToken.None);

        Assert.True(_context.ProviderPays.Count() > initialCount);
    }
}