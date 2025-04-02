using System;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Data.Entities;
using Signify.PAD.Svc.Core.Models;
using Signify.PAD.Svc.Core.Tests.Utilities;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Commands;

public sealed class DeleteStatusesHandlerTests : IDisposable, IAsyncDisposable
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

    private DeleteStatusesHandler CreateSubject()
        => new(A.Dummy<ILogger<DeleteStatusesHandler>>(), _dbFixture.Context);

    [Fact]
    public async Task Handle_WithStatusesToDelete_DeletesStatuses()
    {
        // Arrange
        const int padId = 1;

        await _dbFixture.Context.PADStatus.AddRangeAsync(
            new PADStatus
            {
                PADId = padId, PADStatusCodeId = (int)StatusCodes.PadPerformed
            }, new PADStatus
            {
                PADId = padId, PADStatusCodeId = (int)StatusCodes.WaveformDocumentDownloaded
            }, new PADStatus
            {
                PADId = padId, PADStatusCodeId = (int)StatusCodes.WaveformDocumentUploaded
            });

        await _dbFixture.Context.SaveChangesAsync();

        var beginningStatusCount = await _dbFixture.Context.PADStatus.CountAsync(each => each.PADId == padId);

        var request = new DeleteStatuses(padId, [StatusCodes.WaveformDocumentDownloaded, StatusCodes.WaveformDocumentUploaded]);

        // Act
        await CreateSubject().Handle(request, CancellationToken.None);

        // Assert
        Assert.Equal(beginningStatusCount - 2, await _dbFixture.Context.PADStatus.CountAsync(each => each.PADId == padId));
    }
}