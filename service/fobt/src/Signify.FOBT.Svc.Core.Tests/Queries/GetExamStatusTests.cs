using FluentAssertions;
using Signify.FOBT.Svc.Core.Queries;
using Signify.FOBT.Svc.Core.Tests.Utilities;
using System.Threading.Tasks;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.Queries;

public class GetExamStatusTests : IClassFixture<MockDbFixture>
{
    private readonly MockDbFixture _mockDbFixture;
    private readonly GetExamStatusHandler _handler;

    public GetExamStatusTests(MockDbFixture mockDbFixture)
    {
        _mockDbFixture = mockDbFixture;
        _handler = new GetExamStatusHandler(_mockDbFixture.Context);
    }

    [Fact]
    public async Task GetExamStatusHandler_GetExamStatus()
    {
        // Arrange
        var request = new GetExamStatus(1, 6);

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        Assert.Equal(result.FOBTId, request.ExamId);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(100, 6)]
    [InlineData(100, 100)]
    public async Task GetExamStatusHandler_DoesNotGetExamStatus(int examId, int examStatusCodeId)
    {
        // Arrange
        var request = new GetExamStatus(examId, examStatusCodeId);

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        result.Should().BeNull();
    }
}