using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Signify.PAD.Svc.Core.Data.Entities;
using Signify.PAD.Svc.Core.Queries;
using Signify.PAD.Svc.Core.Tests.Utilities;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Queries;

public class GetWaveformDocumentVendorByNameTests : IClassFixture<MockDbFixture>
{
    private readonly GetWaveformDocumentVendorByNameHandler _handler;

    public GetWaveformDocumentVendorByNameTests(MockDbFixture mockDbFixture)
    {
        _handler = new GetWaveformDocumentVendorByNameHandler(mockDbFixture.Context, A.Fake<ILogger<GetWaveformDocumentVendorByNameHandler>>());
    }

    [Theory]
    [InlineData("Semler Scientific")]
    public async Task GetWaveformDocumentVendorByName_ReturnsOneResult_Successful(string waveformDocumentVendorName)
    {
        // Assert
        var query = new GetWaveformDocumentVendorByName() { WaveformDocumentVendorName = waveformDocumentVendorName };
        // Act
        var result = await _handler.Handle(query, CancellationToken.None);
        // Assert
        Assert.NotNull(result);
        result.VendorName.Should().Be(waveformDocumentVendorName);
    }

    [Theory]
    [InlineData("KindOf Scientific")]
    [InlineData("Un Scientific")]
    public async Task GetWaveformDocumentVendorByName_Type_DataNotFound(string waveformDocumentVendorName)
    {
        // Assert
        var query = new GetWaveformDocumentVendorByName() { WaveformDocumentVendorName = waveformDocumentVendorName };
        // Act
        var result = await _handler.Handle(query, CancellationToken.None);
        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("Semler Scientific")]
    public async Task GetWaveformDocumentVendorByName_Type_DataFound(string waveformDocumentVendorName)
    {
        // Assert
        var query = new GetWaveformDocumentVendorByName() { WaveformDocumentVendorName = waveformDocumentVendorName };
        // Act
        var result = await _handler.Handle(query, CancellationToken.None);
        // Assert
        result.Should().BeOfType<WaveformDocumentVendor>();
    }

    [Theory]
    [InlineData("KindOf Scientific")]
    [InlineData("Un Scientific")]
    public async Task GetWaveformDocumentVendorByName_Returns_NullResult(string waveformDocumentVendorName)
    {
        // Assert
        var query = new GetWaveformDocumentVendorByName() { WaveformDocumentVendorName = waveformDocumentVendorName };
        // Act
        var result = await _handler.Handle(query, CancellationToken.None);
        // Assert
        Assert.Null(result);
    }
}