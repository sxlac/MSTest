using FakeItEasy;
using FluentAssertions;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Data.Entities;
using Signify.PAD.Svc.Core.Infrastructure;
using Signify.PAD.Svc.Core.Tests.Utilities;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Commands;

public class CreatePadStatusTests : IClassFixture<MockDbFixture>
{
    private readonly CreatePadStatusHandler _handler;
    private readonly MockDbFixture _mockDbFixture;

    public CreatePadStatusTests(MockDbFixture mockDbFixture)
    {
        _mockDbFixture = mockDbFixture;
        var applicationTime = A.Fake<IApplicationTime>();
        _handler = new CreatePadStatusHandler(_mockDbFixture.Context, applicationTime);
    }

    [Fact]
    public async Task Should_Insert_PAD_Status()
    {
        const int padId = 1;
        var status = new CreatePadStatus
        {
            StatusCode = PADStatusCode.PadPerformed,
            PadId = 1
        };

        //Act
        var result = await _handler.Handle(status, CancellationToken.None);

        //Assert
        result.Should().NotBe(null);
        result.PAD.PADId.Should().Be(padId);
    }

    [Fact]
    public async Task Should_Insert_PAD_Status_Count()
    {
        var count = _mockDbFixture.Context.PADStatus.Count();
        const int padId = 1;
        var status = new CreatePadStatus
        {
            StatusCode = PADStatusCode.PadPerformed,
            PadId = padId
        };

        //Act
        await _handler.Handle(status, CancellationToken.None);

        //Assert
        _mockDbFixture.Context.PADStatus.Count().Should().BeGreaterThan(count);
    }
}