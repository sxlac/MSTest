using FluentAssertions;
using Signify.CKD.Svc.Core.Commands;
using Signify.CKD.Svc.Core.Data.Entities;
using Signify.CKD.Svc.Core.Tests.Utilities;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.CKD.Svc.Core.Tests.Commands;

public class CreateCKDStatusTests: IClassFixture<MockDbFixture>
{
    private readonly CreateCKDStatusHandler _createCKDStatusHandler;
    private readonly MockDbFixture _mockDbFixture;

    public CreateCKDStatusTests(MockDbFixture mockDbFixture)
    {
        _mockDbFixture = mockDbFixture;
        _createCKDStatusHandler = new CreateCKDStatusHandler(_mockDbFixture.Context);
    }

    [Fact]
    public async Task Should_Insert_CKD_Status()
    {
        var ckdId = 12;
        var status = new CreateCKDStatus
        {
            StatusCodeId = CKDStatusCode.CKDPerformed.CKDStatusCodeId,
            CKDId = ckdId
        };

        //Act
        var result = await _createCKDStatusHandler.Handle(status, CancellationToken.None);

        //Assert
        result.Should().NotBe(null);
        result.CKDId.Should().Be(ckdId);
    }

    [Fact]
    public async Task Should_Insert_CKD_Status_Count()
    {
        var count = _mockDbFixture.Context.CKDStatus.Count();
        var ckd = new Core.Data.Entities.CKD();
        var status = new CreateCKDStatus
        {
            StatusCodeId = CKDStatusCode.CKDPerformed.CKDStatusCodeId,
            CKDId = ckd.CKDId
        };

        //Act
        await _createCKDStatusHandler.Handle(status, CancellationToken.None);

        //Assert
        _mockDbFixture.Context.CKDStatus.Count().Should().BeGreaterThan(count);
    }

    [Fact]
    public void Should_Compare_CKD_Instances_Success()
    {
        var ckd = new Core.Data.Entities.CKD();
        ckd.CKDId = 21;
        var ckd2 = new Core.Data.Entities.CKD();
        ckd2.CKDId = 21;

        //Act
        var result = ckd.Equals(ckd2);

        //Assert
        Assert.True(result);
    }
    
    [Fact]
    public void Should_Compare_CKD_Instances_False()
    {
        var ckd = new Core.Data.Entities.CKD();
        ckd.CKDId = 21;
        var ckd2 = new Core.Data.Entities.CKD();
        ckd2.CKDId = 22;

        //Act
        var result = ckd.Equals(ckd2);

        //Assert
        Assert.False(result);
    }
}