using FluentAssertions;
using Signify.FOBT.Svc.Core.Commands;
using Signify.FOBT.Svc.Core.Data.Entities;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.Commands;

public class CreateFOBTStatusTests: IClassFixture<Utilities.MockDbFixture>
{
    private readonly CreateFOBTStatusHandler _handler;
    private readonly Utilities.MockDbFixture _mockDbFixture;

    public CreateFOBTStatusTests(Utilities.MockDbFixture mockDbFixture)
    {
        _mockDbFixture = mockDbFixture;
        _handler = new CreateFOBTStatusHandler(_mockDbFixture.Context);
    }

    [Fact]
    public async Task Should_Insert_Fobt_Status()
    {
        var fobt = new Core.Data.Entities.FOBT();
        var status = new CreateFOBTStatus
        {
            StatusCode = FOBTStatusCode.FOBTPerformed,
            FOBT = fobt
        };

        //Act
        var result = await _handler.Handle(status, CancellationToken.None);

        //Assert
        result.Should().NotBe(null);
        result.FOBTId.Should().Be(fobt.FOBTId);
    }

    [Fact]
    public async Task Should_Insert_Fobt_Status_Count()
    {
        var count = _mockDbFixture.Context.FOBTStatus.Count();
        var ckd = new Core.Data.Entities.FOBT();
        var status = new CreateFOBTStatus
        {
            StatusCode = FOBTStatusCode.FOBTPerformed,
            FOBT = ckd
        };

        //Act
        await _handler.Handle(status, CancellationToken.None);

        //Assert
        _mockDbFixture.Context.FOBTStatus.Count().Should().BeGreaterThan(count);
    }

    [Fact]
    public void Should_Compare_Fobt_Instances_Success()
    {
        var ckd = new Core.Data.Entities.FOBT();
        ckd.FOBTId = 21;
        var ckd2 = new Core.Data.Entities.FOBT();
        ckd2.FOBTId = 21;

        //Act
        var result = ckd.Equals(ckd2);

        //Assert
        Assert.True(result);
    }
}