using Microsoft.EntityFrameworkCore;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Data.Entities;
using Signify.PAD.Svc.Core.Tests.Utilities;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Commands;

public sealed class DeleteWaveformDocumentHandlerTests : IDisposable, IAsyncDisposable
{
    private readonly MockDbFixture _dbFixture = new();

    public void Dispose()
    {
        _dbFixture.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _dbFixture.DisposeAsync();
    }

    private DeleteWaveformDocumentHandler CreateSubject()
        => new(_dbFixture.Context);

    [Fact]
    public async Task Handle_Test()
    {
        // Arrange
        await _dbFixture.Context.WaveformDocument.AddRangeAsync(new WaveformDocument(), new WaveformDocument());

        await _dbFixture.Context.SaveChangesAsync();

        var request = new DeleteWaveformDocument(await _dbFixture.Context.WaveformDocument.FirstAsync());

        var count = await _dbFixture.Context.WaveformDocument.CountAsync();

        // Act
        await CreateSubject().Handle(request, CancellationToken.None);

        // Assert
        Assert.Equal(EntityState.Detached, _dbFixture.Context.WaveformDocument.Entry(request.Waveform).State);
    }
}
