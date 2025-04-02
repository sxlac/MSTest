using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Tests.Utilities;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Commands;

public class CreateOrUpdatePADTests : IClassFixture<MockDbFixture>
{
    private readonly IMapper _mapper;
    private readonly CreateOrUpdatePADHandler _handler;
    private readonly MockDbFixture _mockDbFixture;
    public CreateOrUpdatePADTests(MockDbFixture mockDbFixture)
    {
        _mapper = A.Fake<IMapper>();
        var logger = A.Fake<ILogger<CreateOrUpdatePADHandler>>();
        _mockDbFixture = mockDbFixture;
        _handler = new CreateOrUpdatePADHandler(mockDbFixture.Context, _mapper, logger);
    }

    [Fact]
    public async Task Should_Create_Pad_DataCheck()
    {
        A.CallTo(() => _mapper.Map<Core.Data.Entities.PAD>(A<CreateOrUpdatePAD>._)).Returns(CreatePad);
        var pad = await _handler.Handle(CreateOrUpdatePad, CancellationToken.None);
        _mockDbFixture.Context.PAD.Any(x => x.AppointmentId == pad.AppointmentId).Should().BeTrue();
    }
    
    [Fact]
    public async Task Should_Create_Pad_CountTest()
    {
        A.CallTo(() => _mapper.Map<Core.Data.Entities.PAD>(A<CreateOrUpdatePAD>._)).Returns(CreatePad);
        var initialCount = _mockDbFixture.Context.PAD.Count();
        await _handler.Handle(CreateOrUpdatePad, CancellationToken.None);
        _mockDbFixture.Context.PAD.Count().Should().BeGreaterThan(initialCount, "There shd be an insert");
    }
    
    [Fact]
    public async Task Should_Create_Pad()
    {
        A.CallTo(() => _mapper.Map<Core.Data.Entities.PAD>(A<CreateOrUpdatePAD>._)).Returns(CreatePad);
        var initialCount = _mockDbFixture.Context.PAD.Count();
        var pad = await _handler.Handle(CreateOrUpdatePad, CancellationToken.None);
        pad.PADId.Should().BeGreaterThan(initialCount);
    }

    [Fact]
    public async Task Should_Create_Pad_TypeCheck()
    {
        A.CallTo(() => _mapper.Map<Core.Data.Entities.PAD>(A<CreateOrUpdatePAD>._)).Returns(CreatePad);
        var pad = await _handler.Handle(CreateOrUpdatePad, CancellationToken.None);
        pad.Should().BeOfType<Core.Data.Entities.PAD>();
    }

    [Fact]
    public async Task Should_Update_Pad()
    {
        A.CallTo(() => _mapper.Map<Core.Data.Entities.PAD>(A<CreateOrUpdatePAD>.That.Matches(e => e.PADId == 0))).Returns(CreatePad);
        
        A.CallTo(() => _mapper.Map<Core.Data.Entities.PAD>(A<CreateOrUpdatePAD>._)).Returns(CreatePad);
        var before = await _handler.Handle(CreateOrUpdatePad, CancellationToken.None);

        var updated = CreateOrUpdatePad;
        updated.PADId = before.PADId;
        var updatePad = CreatePad;
        updatePad.UserName = "updated";
        updatePad.PADId = before.PADId;

        _mockDbFixture.Context.ChangeTracker.Clear();

        A.CallTo(() => _mapper.Map<Core.Data.Entities.PAD>(A<CreateOrUpdatePAD>.That.Matches(e => e.PADId == before.PADId))).Returns(updatePad);

        var after = await _handler.Handle(updated, CancellationToken.None);
        Assert.Equal(before.PADId, after.PADId);
        Assert.NotEqual(before.UserName, after.UserName);
    }
       
    private static Core.Data.Entities.PAD CreatePad => new()
    {
        AddressLineOne = "Raghavendra nagara",
        AddressLineTwo = "mysuru",
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
        RightScoreAnswerValue = "0.59",
        RightSeverityAnswerValue = "Significant",
        LeftScoreAnswerValue = "0.28",
        LeftSeverityAnswerValue = "Severe",
        CenseoId = "ADarsh1234",
        City = "Mysuru",
        DateOfBirth = DateTime.UtcNow,
        FirstName = "Adarsh",
        LastName = "H R",
        NationalProviderIdentifier = "1234567890",
        State = "karnataka",
        UserName = "ADarsh",
        ZipCode = "12345"
    };
    
    private static CreateOrUpdatePAD CreateOrUpdatePad => new()
    {
        AddressLineOne = "Raghavendra nagara",
        AddressLineTwo = "mysuru",
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
        RightScoreAnswerValue = "0.59",
        RightSeverityAnswerValue = "Significant",
        LeftScoreAnswerValue = "0.28",
        LeftSeverityAnswerValue = "Severe",
        CenseoId = "ADarsh1234",
        City = "Mysuru",
        DateOfBirth = DateTime.UtcNow,
        FirstName = "Adarsh",
        LastName = "H R",
        NationalProviderIdentifier = "1234567890",
        State = "karnataka",
        UserName = "ADarsh",
        ZipCode = "12345"
    };
}