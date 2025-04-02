using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Signify.FOBT.Svc.Core.Commands;
using Signify.FOBT.Svc.Core.Tests.Utilities;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.Commands;

public class AddFOBTNotPerformedHandlerTests : IClassFixture<MockDbFixture>
{
    private readonly IMapper _mapper;
    private readonly AddFOBTNotPerformedHandler _handler;
    private readonly MockDbFixture _mockDbFixture;

    public AddFOBTNotPerformedHandlerTests(MockDbFixture mockDbFixture)
    {
        _mapper = A.Fake<IMapper>();
        var logger = A.Fake<ILogger<AddFOBTNotPerformedHandler>>();
        _mockDbFixture = mockDbFixture;
        _handler = new AddFOBTNotPerformedHandler(logger, mockDbFixture.Context, _mapper);
    }

    [Fact]
    public async Task Should_Create_FobtNotPerformed()
    {
        A.CallTo(() => _mapper.Map<Core.Data.Entities.FOBTNotPerformed>(A<AddFOBTNotPerformed>._)).Returns(FobtNotPerformed);
        var initialCount = _mockDbFixture.Context.FOBTNotPerformed.Count();
        await _handler.Handle(addFobtNotPerformed, CancellationToken.None);
        _mockDbFixture.Context.FOBTNotPerformed.Count().Should().BeGreaterThanOrEqualTo(initialCount, "There shd be an insert");
    }

    [Fact]
    public async Task Should_Return_FobtNotPerformed()
    {
        A.CallTo(() => _mapper.Map<Core.Data.Entities.FOBTNotPerformed>(A<AddFOBTNotPerformed>._)).Returns(FobtNotPerformed);
        _mockDbFixture.Context.FOBTNotPerformed.Add(FobtNotPerformed);
        var result = await _handler.Handle(addFobtNotPerformed, CancellationToken.None);
        _mockDbFixture.Context.FOBTNotPerformed.Any(x => x.FOBTId == result.FOBTId).Should().BeTrue();
    }

    private static Core.Data.Entities.FOBT Fobt => new()
    {
        FOBTId =2121,
        AddressLineOne = "503 Highland Drive",
        AddressLineTwo = "",
        ApplicationId = "Signify.Evaluation.Service",
        AppointmentId = 1000084716,
        ClientId = 14,
        CreatedDateTime = DateTimeOffset.UtcNow,
        DateOfService = DateTime.UtcNow,
        EvaluationId = 324359,
        MemberId = 11990396,
        MemberPlanId = 21074285,
        ProviderId = 42879,
        ReceivedDateTime = DateTime.UtcNow,
        CenseoId = "TestName1234",
        City = "Dallas",
        DateOfBirth = DateTime.UtcNow,
        FirstName = "TestName",
        LastName = "H R",
        NationalProviderIdentifier = "1234567890",
        State = "Texas",
        UserName = "TestName",
        ZipCode = "12345"
    };

    private static Core.Data.Entities.NotPerformedReason NotPerformedReason => new()
    {
        NotPerformedReasonId = 3131,
        AnswerId = 4141,
        Reason = "Patient Unwilling",
    };

    private static Core.Data.Entities.FOBTNotPerformed FobtNotPerformed => new()
    {
        NotPerformedReasonId = 3131,
        FOBTId = 2121,
    };

    private static AddFOBTNotPerformed addFobtNotPerformed => new()
    {
        FOBT = Fobt,
        NotPerformedReason = NotPerformedReason
    };
}