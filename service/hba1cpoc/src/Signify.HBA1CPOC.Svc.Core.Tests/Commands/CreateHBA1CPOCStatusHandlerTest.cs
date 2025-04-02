using FakeItEasy;
using FluentAssertions;
using Signify.HBA1CPOC.Svc.Core.Commands;
using Signify.HBA1CPOC.Svc.Core.Data.Entities;
using Signify.HBA1CPOC.Svc.Core.Infrastructure;
using Signify.HBA1CPOC.Svc.Core.Tests.Utilities;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.HBA1CPOC.Svc.Core.Tests.Commands;

public  class CreateHBA1CPOCStatusHandlerTest : IClassFixture<MockDbFixture>
{
    private readonly CreateHBA1CPOCStatusHandler _createHBA1CPOCStatusHandler;
    private readonly MockDbFixture _mockDbFixture;

    public CreateHBA1CPOCStatusHandlerTest(MockDbFixture mockDbFixture)
    {
        _mockDbFixture = mockDbFixture;
        _createHBA1CPOCStatusHandler = new CreateHBA1CPOCStatusHandler(_mockDbFixture.Context, A.Fake<IApplicationTime>());
    }

    [Fact]
    public async Task Should_Insert_HBA1CPOC_Status()
    {
        var hba1Cpoc = new Core.Data.Entities.HBA1CPOC();
        var status = new CreateHBA1CPOCStatus()
        {
            StatusCodeId = HBA1CPOCStatusCode.HBA1CPOCPerformed.HBA1CPOCStatusCodeId,
            HBA1CPOCId = hba1Cpoc.HBA1CPOCId
        };

        //Act
        var result = await _createHBA1CPOCStatusHandler.Handle(status, CancellationToken.None);

        //Assert
        result.Should().NotBe(null);
        result.HBA1CPOCId.Should().Be(hba1Cpoc.HBA1CPOCId);
    }

    [Fact]
    public async Task Should_Insert_HBA1CPOC_Status_Count()
    {
        var count = _mockDbFixture.Context.HBA1CPOCStatus.Count();
        var hba1Cpoc = new Core.Data.Entities.HBA1CPOC();
        var status = new CreateHBA1CPOCStatus
        {
            StatusCodeId = HBA1CPOCStatusCode.HBA1CPOCPerformed.HBA1CPOCStatusCodeId,
            HBA1CPOCId = hba1Cpoc.HBA1CPOCId
        };

        //Act
        var result = await _createHBA1CPOCStatusHandler.Handle(status, CancellationToken.None);

        //Assert
        _mockDbFixture.Context.HBA1CPOCStatus.Count().Should().BeGreaterThan(count);
    }

    [Fact]
    public void Should_Compare_HBA1CPOC_Instances_Success()
    {
        var hba1Cpoc = new Core.Data.Entities.HBA1CPOC();
        hba1Cpoc.HBA1CPOCId = 21;
        var hba1Cpoc2 = new Core.Data.Entities.HBA1CPOC();
        hba1Cpoc2.HBA1CPOCId = 21;

        //Act
        var result = hba1Cpoc.Equals(hba1Cpoc2);

        //Assert
        Assert.True(result);
    }
}