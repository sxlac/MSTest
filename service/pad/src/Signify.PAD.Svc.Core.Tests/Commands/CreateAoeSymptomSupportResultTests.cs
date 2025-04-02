using FakeItEasy;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Data.Entities;
using Signify.PAD.Svc.Core.Infrastructure;
using Signify.PAD.Svc.Core.Tests.Utilities;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Commands;

public class CreateAoeSymptomSupportResultTests : IClassFixture<MockDbFixture>
{
    private readonly MockDbFixture _mockDbFixture;
    private readonly CreateAoeSymptomSupportResultHandler _handler;

    public CreateAoeSymptomSupportResultTests(MockDbFixture mockDbFixture)
    {
        _mockDbFixture = mockDbFixture;
        var applicationTime = A.Fake<IApplicationTime>();
        _handler = new CreateAoeSymptomSupportResultHandler(_mockDbFixture.Context, applicationTime);
    }

    [Fact]
    public async Task Handle_AddingNewAoeSymptomSupportResult_SaveNewRecordToDb()
    {
        // Arrange
        var count = _mockDbFixture.Context.AoeSymptomSupportResult.Count();
        var request = new CreateAoeSymptomSupportResult
        {
            AoeSymptomSupportResult = new AoeSymptomSupportResult
            {
                AoeSymptomSupportResultId = 1,
                PADId = 1,
                CreatedDateTime = DateTime.Now,
                FootPainDisappearsOtc = true,
                FootPainDisappearsWalkingOrDangling = true,
                FootPainRestingElevatedLateralityCodeId = 1,
                PedalPulseCodeId = 1
            }
        };

        // Act
        await _handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.Equal(_mockDbFixture.Context.AoeSymptomSupportResult.Count(), count + 1);
    }
}
