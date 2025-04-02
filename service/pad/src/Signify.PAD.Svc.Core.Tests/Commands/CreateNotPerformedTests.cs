using FluentAssertions;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Tests.Utilities;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Commands;

public class CreateNotPerformedTests: IClassFixture<MockDbFixture>
{
    private readonly CreateNotPerformedHandler _handler;
    private readonly MockDbFixture _mockDbFixture;

    public CreateNotPerformedTests(MockDbFixture mockDbFixture)
    {
        _mockDbFixture = mockDbFixture;
        _handler = new CreateNotPerformedHandler(_mockDbFixture.Context);
    }

    [Fact]
    public async Task Should_Insert_NotPerformed_Record()
    {
        //Arrange
        var notPerformed = new CreateNotPerformed
        {
            NotPerformedRec = new Core.Data.Entities.NotPerformed
            {
                PADId = 1,
                AnswerId = 234,
                Notes = "Test"
            }
        };

        //Act
        var result = await _handler.Handle(notPerformed, CancellationToken.None);

        //Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Should_Insert_Not_Performed_Count()
    {
        //Arrange
        var count = _mockDbFixture.Context.NotPerformed.Count();
        var notPerformed = new CreateNotPerformed
        {
            NotPerformedRec = new Core.Data.Entities.NotPerformed
            {
                PADId = 1,
                AnswerId = 234,
                Notes = "Test"
            }
        };

        //Act
        await _handler.Handle(notPerformed, CancellationToken.None);

        //Assert
        _mockDbFixture.Context.NotPerformed.Count().Should().BeGreaterThan(count);
    }
}