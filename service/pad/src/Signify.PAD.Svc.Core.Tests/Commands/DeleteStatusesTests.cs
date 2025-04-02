using System;
using System.Collections.Generic;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Models;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Commands;

public class DeleteStatusesTests
{
    [Fact]
    public void Construct_WithUnsupportedStatusCode_Throws()
    {
        // Arrange
        const int padId = 1;

        var supported = new HashSet<StatusCodes>([StatusCodes.WaveformDocumentDownloaded, StatusCodes.WaveformDocumentUploaded]);

        foreach (var statusCode in Enum.GetValues<StatusCodes>())
        {
            // Act
            // Assert
            if (!supported.Contains(statusCode))
                Assert.Throws<NotSupportedException>(() => new DeleteStatuses(padId, [statusCode]));
            else
                _ = new DeleteStatuses(padId, [statusCode]);
        }
    }

    [Fact]
    public void Construct_WithNoStatusCodes_Throws()
    {
        // Arrange
        const int padId = 1;

        // Act
        // Assert
        Assert.Throws<InvalidOperationException>(() => new DeleteStatuses(padId, []));
    }

    [Fact]
    public void Construct_WithSupportedStatuses_SetsProperties()
    {
        // Arrange
        const int padId = 1;

        // Act
        var subject = new DeleteStatuses(padId, [
            StatusCodes.WaveformDocumentDownloaded,
            StatusCodes.WaveformDocumentDownloaded, // should just result in 2 total
            StatusCodes.WaveformDocumentUploaded
        ]);

        // Assert
        Assert.Equal(padId, subject.PadId);
        Assert.Equal(2, subject.StatusCodeIds.Count);

        Assert.Contains((int)StatusCodes.WaveformDocumentDownloaded, subject.StatusCodeIds);
        Assert.Contains((int)StatusCodes.WaveformDocumentUploaded, subject.StatusCodeIds);
    }
}
