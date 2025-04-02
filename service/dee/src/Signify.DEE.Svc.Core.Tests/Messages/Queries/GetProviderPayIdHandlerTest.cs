using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Messages.Queries;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Messages.Queries;

public class GetProviderPayIdHandlerTest 
{
    private readonly GetProviderPayIdHandler _getProviderPayId;
    private readonly DataContext _context;
    private static readonly FakeApplicationTime ApplicationTime = new();

    public GetProviderPayIdHandlerTest()
    {
        var options = new DbContextOptionsBuilder<DataContext>().UseInMemoryDatabase(databaseName: "DEE_getProviderPayId").Options;

        var logger = A.Fake<ILogger<GetProviderPayIdHandler>>();
        _context = new DataContext(options);

        _getProviderPayId = new GetProviderPayIdHandler(logger, _context);
    }

    [Fact]
    public async Task Payment_Id_Should_Have_Value()
    {
        await _context.ProviderPays.AddAsync(ProviderPay);
        await _context.SaveChangesAsync();
        var getPaymentId = new GetProviderPayId(1);

        var actualResult = await _getProviderPayId.Handle(getPaymentId, CancellationToken.None);
        actualResult.Should().NotBe(null);
        actualResult.Should().NotBe(string.Empty);
    }

    [Fact]
    public async Task Payment_Id_Should_Not_Have_Value()
    {
        var getPaymentId = new GetProviderPayId(2);
        var actualResult = await _getProviderPayId.Handle(getPaymentId, CancellationToken.None);
        actualResult.Should().Be(string.Empty);
    }

    private static Core.Data.Entities.ProviderPay ProviderPay => new()
    {
        PaymentId = "FakePaymentId",
        CreatedDateTime = ApplicationTime.UtcNow(),
        ExamId = 1,
        Id = 1,
    };
}