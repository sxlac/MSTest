using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.PAD.Svc.Core.Data.Entities;
using Signify.PAD.Svc.Core.Queries;
using Signify.PAD.Svc.Core.Tests.Utilities;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Queries;

public sealed class QueryWaveformsHandlerTests : IDisposable, IAsyncDisposable
{
    private readonly MockDbFixture _dbFixture = new();

    public void Dispose()
    {
        _dbFixture?.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _dbFixture.DisposeAsync();
    }

    private QueryWaveformsHandler CreateSubject()
        => new(A.Dummy<ILogger<QueryWaveformsHandler>>(), _dbFixture.Context);

    [Fact]
    public async Task Handle_WithVariousWaveformDocuments_Test()
    {
        // Arrange
        const string goodVendorName = nameof(goodVendorName);
        const string badVendorName = nameof(badVendorName);

        var query = new QueryWaveforms(goodVendorName, DateTime.UtcNow, DateTime.UtcNow.AddHours(1));

        var goodVendor = new WaveformDocumentVendor
        {
            VendorName = goodVendorName
        };
        var badVendor = new WaveformDocumentVendor
        {
            VendorName = badVendorName
        };

        var documents = new WaveformDocument[]
        {
            new()
            {
                WaveformDocumentVendor = badVendor, // doesn't match
                CreatedDateTime = query.StartDateTime // matches
            },
            new()
            {
                WaveformDocumentVendor = goodVendor, // matches
                CreatedDateTime = query.StartDateTime.AddSeconds(-1) // doesn't match
            },
            new()
            {
                WaveformDocumentVendor = goodVendor, // matches
                CreatedDateTime = query.StartDateTime // matches
            },
            new()
            {
                WaveformDocumentVendor = goodVendor, // matches
                CreatedDateTime = query.EndDateTime.AddSeconds(1) // doesn't match
            }
        };

        await _dbFixture.Context.WaveformDocumentVendor.AddRangeAsync(goodVendor, badVendor);
        await _dbFixture.Context.WaveformDocument.AddRangeAsync(documents);

        await _dbFixture.Context.SaveChangesAsync();
        
        // Act
        var result = await CreateSubject().Handle(query, CancellationToken.None);

        // Assert
        Assert.Single(result);
    }
}
