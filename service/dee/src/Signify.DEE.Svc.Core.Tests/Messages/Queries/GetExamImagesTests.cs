using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Signify.DEE.Svc.Core.Messages.Queries;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Messages.Queries;

public class GetExamImagesTests : IClassFixture<MockDbFixture>
{
    private readonly GetExamImagesHandler _handler;

    public GetExamImagesTests(MockDbFixture mockDbFixture)
    {
        var logger = A.Fake<ILogger<GetExamImagesHandler>>();

        _handler = new GetExamImagesHandler(logger, mockDbFixture.FakeDatabaseContext);
    }

    [Fact]
    public async Task GetExamImagesHandler_ReturnEmpty_WhenNoRecordExists()
    {
        // Arrange
        var request = new GetExamImages { ExamId = 1 };

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        result.Should().NotBeNull();
        result.Count().Should().Be(0);
    }

    [Fact]
    public async Task GetExamImagesHandler_ReturnAllImages_WhenExamHasImages()
    {
        // Arrange
        var request = new GetExamImages { ExamId = 45648 };

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        result.Should().NotBeNull();
        result.Count().Should().Be(2);
    }
}