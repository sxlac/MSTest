using FluentAssertions;
using Signify.PAD.Svc.Core.Queries;
using Signify.PAD.Svc.Core.Tests.Utilities;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Queries;

public class GetWaveformDocumentByFilenameTests : IClassFixture<MockDbFixture>
{
    private readonly GetWaveformDocumentByFilenameHandler _handler;

    public GetWaveformDocumentByFilenameTests(MockDbFixture mockDbFixture)
    {
        _handler = new GetWaveformDocumentByFilenameHandler(mockDbFixture.Context);
    }

    [Theory]
    [InlineData("WALKER_122940331_PAD_BL_080122.PDF")]
    public async Task GetWaveformDocumentByFilename_ReturnsOneResult_Successful(string filename)
    {
        // Arrange
        var query = new GetWaveformDocumentByFilename { Filename = filename };
        // Act
        var result = await _handler.Handle(query, CancellationToken.None);
        // Assert
        Assert.NotNull(result);
        Assert.Equal(filename, result.Filename);
    }

    [Theory]
    [InlineData("WALKER_122940331_PAD_BL_080122.XLS")]
    [InlineData("WALKER_122940331_PAD_BL_080122.DOC")]
    public async Task GetWaveformDocumentByFilename_WhenNotFound_ReturnsNull(string filename)
    {
        // Arrange
        var query = new GetWaveformDocumentByFilename { Filename = filename };
        // Act
        var result = await _handler.Handle(query, CancellationToken.None);
        // Assert
        result.Should().BeNull();
    }
}