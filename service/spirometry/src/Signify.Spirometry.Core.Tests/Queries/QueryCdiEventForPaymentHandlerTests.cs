using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Queries;
using System.Threading.Tasks;
using System;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Queries;

public class QueryCdiEventForPaymentHandlerTests
{
    [Fact]
    public async Task Handle_WhenEntityDoesNotExist_ReturnsNull()
    {
        var requestId = Guid.NewGuid();
        await using var fixture = new MockDbFixture();
        var subject = new QueryCdiEventForPaymentHandler(fixture.SharedDbContext);

        var result = await subject.Handle(new QueryCdiEventForPayment(requestId), default);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_WhenEntityExists_ReturnsEntity()
    {
        var requestId = Guid.NewGuid();
        await using var fixture = new MockDbFixture();
        var cdiEvent = new CdiEventForPayment
        {
            EvaluationId = 1234,
            RequestId = requestId,
            DateTime = new FakeApplicationTime().UtcNow(),
            EventType = "CDIFailedEvent",
            PayProvider = false,
            ApplicationId = "DpsUser"
        };
        await fixture.SharedDbContext.CdiEventForPayments.AddAsync(cdiEvent);
        await fixture.SharedDbContext.SaveChangesAsync();
        var subject = new QueryCdiEventForPaymentHandler(fixture.SharedDbContext);

        var result = await subject.Handle(new QueryCdiEventForPayment(requestId), default);

        Assert.NotNull(result);
    }
}