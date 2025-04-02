using FakeItEasy;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Messages.Queries;
using System.Threading.Tasks;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Messages.Queries;

public class GetExamResultByExamIdTests
{
    private readonly DataContext _context;
    private readonly GetExamResultByExamIdHandler _handler;

    public GetExamResultByExamIdTests()
    {
        var options = new DbContextOptionsBuilder<DataContext>().UseInMemoryDatabase(databaseName: "DEE_Get_Exam_Results_By_Exam_Id_Test").Options;
        _context = new DataContext(options);
        _handler = new(A.Fake<ILogger<GetExamResultByExamIdHandler>>(), _context);
        SeedData();
    }

    void SeedData()
    {
        _context.ExamResults.Add(new Core.Data.Entities.ExamResult() { ExamId = 1234 });
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetExamResultsByExamId_ReturnsEntity_WhenExamIdMatched()
    {
        // Arrange
        var request = new GetExamResultByExamId { ExamId = 1234 };

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetExamResultsByExamId_ReturnsNull_WhenExamIdNotMatched()
    {
        // Arrange
        var request = new GetExamResultByExamId { ExamId = 1235 };

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        result.Should().BeNull();
    }
}