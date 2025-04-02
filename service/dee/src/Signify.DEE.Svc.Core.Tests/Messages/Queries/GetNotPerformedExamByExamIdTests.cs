using FakeItEasy;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Messages.Queries;
using System.Threading.Tasks;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Messages.Queries;

public class GetNotPerformedExamByExamIdTests
{
    private readonly DataContext _context;
    private readonly GetNotPerformedExamByExamIdHandler _handler;

    public GetNotPerformedExamByExamIdTests()
    {
        var options = new DbContextOptionsBuilder<DataContext>().UseInMemoryDatabase(databaseName: "DEE_Get_Not_Performed_By_Exam_Id_Test").Options;
        _context = new DataContext(options);
        _handler = new(A.Fake<ILogger<GetNotPerformedExamByExamIdHandler>>(), _context);
        SeedData();
    }

    void SeedData()
    {
        _context.DeeNotPerformed.Add(new Core.Data.Entities.DeeNotPerformed() { ExamId = 1234 });
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetNotPerformedExamByExamId_ReturnsEntity_WhenExamIdMatched()
    {
        // Arrange
        var request = new GetNotPerformedExamByExamId { ExamId = 1234 };

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetNotPerformedExamByExamId_ReturnsNull_WhenExamIdNotMatched()
    {
        // Arrange
        var request = new GetNotPerformedExamByExamId { ExamId = 1235 };

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        result.Should().BeNull();
    }
}