using Signify.HBA1CPOC.Svc.Core.Data.Entities;
using Signify.HBA1CPOC.Svc.Core.Queries;
using Signify.HBA1CPOC.Svc.Core.Tests.Utilities;
using System.Threading.Tasks;
using System;
using Xunit;

namespace Signify.HBA1CPOC.Svc.Core.Tests.Queries;

public class QueryExamStatusesHandlerTests
{
    [Fact]
    public async Task Handle_WhenNoStatusesExist_ReturnsEmptyCollection()
    {
        // Arrange
        const int examId = 99;

        var request = new QueryExamStatuses
        {
            ExamId = examId
        };

        await using var fixture = new MockDbFixture();

        // Act
        var result = await new QueryExamStatusesHandler(fixture.Context).Handle(request, default);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_WhenStatusesExist_ReturnsStatuses()
    {
        // Arrange
        const int examId = 99;

        var request = new QueryExamStatuses
        {
            ExamId = examId
        };

        await using var fixture = new MockDbFixture();

        await fixture.Context.HBA1CPOCStatus.AddAsync(new HBA1CPOCStatus(98, HBA1CPOCStatusCode.BillRequestSent,
            new Core.Data.Entities.HBA1CPOC
            {
                HBA1CPOCId = examId
            }, DateTimeOffset.UtcNow));
        await fixture.Context.SaveChangesAsync();

        // Act
        var result = await new QueryExamStatusesHandler(fixture.Context).Handle(request, default);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
    }
}