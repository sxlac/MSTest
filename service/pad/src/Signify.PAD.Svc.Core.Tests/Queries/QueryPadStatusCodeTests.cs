using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.PAD.Svc.Core.Data.Entities;
using Signify.PAD.Svc.Core.Queries;
using Signify.PAD.Svc.Core.Tests.Utilities;
using System.Threading.Tasks;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Queries;

public class QueryPadStatusCodeTests : IClassFixture<MockDbFixture>
{
    private readonly QueryPadStatusCodeHandler _subject;

    public QueryPadStatusCodeTests(MockDbFixture fixture)
    {
        _subject = new QueryPadStatusCodeHandler(A.Dummy<ILogger<QueryPadStatusCode>>(), fixture.Context);
    }

    [Fact]
    public async Task Handle_NotWaveformDocumentDownloaded_ReturnsFalse()
        => Assert.False(await _subject.Handle(new QueryPadStatusCode(1, PADStatusCode.WaveformDocumentDownloaded), default));

    [Fact]
    public async Task Handle_IsPADPerformed_ReturnsTrue()
        => Assert.True(await _subject.Handle(new QueryPadStatusCode(1, PADStatusCode.PadPerformed), default));

    [Fact]
    public async Task Handle_IsWaveformDocumentUploaded_ReturnsTrue()
        => Assert.True(await _subject.Handle(new QueryPadStatusCode(3, PADStatusCode.WaveformDocumentUploaded), default));
}