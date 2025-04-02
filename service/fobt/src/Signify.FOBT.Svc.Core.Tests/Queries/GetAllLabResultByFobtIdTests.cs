using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Signify.FOBT.Svc.Core.Data.Entities;
using Signify.FOBT.Svc.Core.Data;
using Signify.FOBT.Svc.Core.Queries;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.Queries;

public class GetAllLabResultByFobtIdTests
{
    private readonly GetAllLabResultsByFobtIdHandler _handler;
    private readonly FOBTDataContext _contextInMemory;

    public GetAllLabResultByFobtIdTests()
    {
        var options = new DbContextOptionsBuilder<FOBTDataContext>()
            .UseInMemoryDatabase(databaseName: "FOBT-GetAllLabResults").Options;
        _contextInMemory = new FOBTDataContext(options);
        _handler = new GetAllLabResultsByFobtIdHandler(_contextInMemory);
    }

    [Fact]
    public async Task GetAllLabResultByFobtIdHandler_ReturnsLabResults()
    {
        // Arrange
        const int fobtId = 1;
        var query = new GetAllLabResultsByFobtId() { FobtId = fobtId };

        _contextInMemory.LabResults.AddRange(GetLabResults());
        await _contextInMemory.SaveChangesAsync();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }


    [Fact]
    public async Task GetLabResultByFobtIdHandler_WhenNoValueFound_ReturnNull()
    {
        // Arrange
        var request = new GetAllLabResultsByFobtId
        {
            FobtId = 9876
        };

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        result.Should().BeEmpty();
        result.Should().BeOfType(typeof(List<LabResults>));
    }

    private static IEnumerable<LabResults> GetLabResults()
    {
        var result = new List<LabResults>
        {
            new() { FOBTId = 1, LabResultId = 123 },
            new() { FOBTId = 1, LabResultId = 456 },
            new() { FOBTId = 2, LabResultId = 789 },
        };
        return result;
    }
}