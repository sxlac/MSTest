using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Data.Entities;
using Signify.PAD.Svc.Core.Tests.Utilities;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Commands;

public class AddExamStatusTests : IClassFixture<MockDbFixture>
{
    private readonly AddExamStatusHandler _handler;

    public AddExamStatusTests(MockDbFixture mockDbFixture)
    {
        _handler = new AddExamStatusHandler(mockDbFixture.Context);
    }

    [Fact]
    public async Task Should_Create_PadStatus()
    {
        var padStatus = new PADStatus(100, 100, 1, DateTimeOffset.Now);
        var request = new AddExamStatus(padStatus);
        var result = await _handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(1, result.Status.PADId);
        Assert.Equal(1, result.Status.PAD.PADId);
    }
}