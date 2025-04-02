using FluentAssertions;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Data.Entities;
using Signify.PAD.Svc.Core.Tests.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Commands;

public class CreateWaveformDocumentTests : IClassFixture<MockDbFixture>
{
    private readonly CreateWaveformDocumentHandler _handler;
    private readonly MockDbFixture _mockDbFixture;

    public CreateWaveformDocumentTests(MockDbFixture mockDbFixture)
    {
        _mockDbFixture = mockDbFixture;
        _handler = new CreateWaveformDocumentHandler(mockDbFixture.Context);
    }

    [Theory]
    [MemberData(nameof(WaveformDocument_Create_Entity_TestData))]
    public async Task CreateWaveformDocument_ValidateEntityCreation_DocumentIdMatches(CreateWaveformDocument createWaveformDocument)
    {
        var entity = await _handler.Handle(createWaveformDocument, CancellationToken.None);
        _mockDbFixture.Context.WaveformDocument.Any(x => x.WaveformDocumentId == entity.WaveformDocumentId).Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(WaveformDocument_Create_Entity_TestData))]
    public async Task CreateWaveformDocument_ValidateEntityCreation_EntityCountIncreased(CreateWaveformDocument createWaveformDocument)
    {
        var initialCount = _mockDbFixture.Context.WaveformDocument.Count();
        await _handler.Handle(createWaveformDocument, CancellationToken.None);
        _mockDbFixture.Context.WaveformDocument.Count().Should().BeGreaterThan(initialCount, "There should be an insert");
    }

    [Theory]
    [MemberData(nameof(WaveformDocument_Create_Entity_TestData))]
    public async Task CreateWaveformDocument_ValidateEntityCreation_DocumentIdIncremented(CreateWaveformDocument createWaveformDocument)
    {
        var initialCount = _mockDbFixture.Context.WaveformDocument.Count();
        var entity = await _handler.Handle(createWaveformDocument, CancellationToken.None);
        entity.WaveformDocumentId.Should().BeGreaterThan(initialCount);
    }

    public static IEnumerable<object[]> WaveformDocument_Create_Entity_TestData()
    {
        var waveformDocument = new WaveformDocument
        {
            WaveformDocumentVendorId = 1,
            Filename = "WALKER_122940330_PAD_BL_080122.PDF",
            CreatedDateTime = DateTime.UtcNow
        };

        var createWaveformDocument = new CreateWaveformDocument(waveformDocument);

        yield return new object[] { createWaveformDocument };
    }
}