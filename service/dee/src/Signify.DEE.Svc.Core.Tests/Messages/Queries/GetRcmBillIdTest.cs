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

public class GetRcmBillIdTest : IClassFixture<Utilities.EntityFixtures>
{
    private readonly GetRcmBillIdHandler _geRcmBillIdHandler;
    private readonly DataContext _context;
    private static readonly FakeApplicationTime ApplicationTime = new();

    public GetRcmBillIdTest()
    {
        var options = new DbContextOptionsBuilder<DataContext>().UseInMemoryDatabase(databaseName: "DEE_getRcmBillId").Options;
        var logger = A.Fake<ILogger<GetRcmBillIdHandler>>();
        _context = new DataContext(options);
        _geRcmBillIdHandler = new GetRcmBillIdHandler(logger, _context);
    }

    /// <summary>
    /// Response type
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task Bill_Id_Should_Have_Value()
    {
        await _context.DEEBilling.AddAsync(DeeBilling);
        await _context.SaveChangesAsync();
        var getRcmBillId = new GetRcmBillId(1);
        var actualResult = await _geRcmBillIdHandler.Handle(getRcmBillId, CancellationToken.None);

        actualResult.Should().NotBe(null);
        actualResult.Should().NotBe(string.Empty);
    }

    [Fact]
    public async Task Bill_Id_Should_Not_Have_Value()
    {
        var getRcmBillId = new GetRcmBillId(2);
        var actualResult = await _geRcmBillIdHandler.Handle(getRcmBillId, CancellationToken.None);

        actualResult.Should().Be(string.Empty);
    }

    private static Core.Data.Entities.DEEBilling DeeBilling => new()
    {
        BillId = "FakeBillId",
        CreatedDateTime = ApplicationTime.UtcNow(),
        ExamId = 1,
        Id = 1,
    };
}