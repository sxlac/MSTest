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

public class CreateOrUpdateFOBTTests : IClassFixture<MockDbFixture>
{
    private readonly IMapper _mapper;
    private readonly CreateOrUpdateFOBTHandler _handler;
    private readonly MockDbFixture _mockDbFixture;
    public CreateOrUpdateFOBTTests(MockDbFixture mockDbFixture)
    {
        _mapper = A.Fake<IMapper>();
        var logger = A.Fake<ILogger<CreateOrUpdateFOBTHandler>>();
        _mockDbFixture = mockDbFixture;
        _handler = new CreateOrUpdateFOBTHandler(mockDbFixture.Context, _mapper, logger);
    }

    [Fact]
    public async Task Should_Create_Fobt_DataCheck()
    {
        A.CallTo(() => _mapper.Map<Core.Data.Entities.FOBT>(A<CreateOrUpdateFOBT>._)).Returns(CreateFobt);
        var result = await _handler.Handle(CreateOrUpdateFobt, CancellationToken.None);
        _mockDbFixture.Context.FOBT.ToList().Any(x => x.AppointmentId == result.AppointmentId).Should().BeTrue();
    }

    [Fact]
    public async Task Should_Create_Fobt_CountTest()
    {
        A.CallTo(() => _mapper.Map<Core.Data.Entities.FOBT>(A<CreateOrUpdateFOBT>._)).Returns(CreateFobt);
        var initialCount = _mockDbFixture.Context.FOBT.Count();
        await _handler.Handle(CreateOrUpdateFobt, CancellationToken.None);
        _mockDbFixture.Context.FOBT.Count().Should().BeGreaterThan(initialCount, "There shd be an insert");
    }

    [Fact]
    public async Task Should_Create_Fobt()
    {
        A.CallTo(() => _mapper.Map<Core.Data.Entities.FOBT>(A<CreateOrUpdateFOBT>._)).Returns(CreateFobt);
        var initialCount = _mockDbFixture.Context.FOBT.Count();
        var result = await _handler.Handle(CreateOrUpdateFobt, CancellationToken.None);
        result.FOBTId.Should().BeGreaterThan(initialCount);
    }

    [Fact]
    public async Task Should_Create_Fobt_TypeCheck()
    {
        A.CallTo(() => _mapper.Map<Core.Data.Entities.FOBT>(A<CreateOrUpdateFOBT>._)).Returns(CreateFobt);
        var initialCount = _mockDbFixture.Context.FOBT.Count();
        var result = await _handler.Handle(CreateOrUpdateFobt, CancellationToken.None);
        var finalCount = _mockDbFixture.Context.FOBT.Count();
        result.Should().BeOfType<Core.Data.Entities.FOBT>();
        Assert.True(initialCount < finalCount);
    }

    [Fact]
    public async Task Should_Update_Fobt_DataCheck()
    {
        A.CallTo(() => _mapper.Map<Core.Data.Entities.FOBT>(A<CreateOrUpdateFOBT>._)).Returns(CreateFobt);
        var initialCount = _mockDbFixture.Context.FOBT.Count();
        var result = await _handler.Handle(CreateOrUpdateFobt, CancellationToken.None);
        var finalCount = _mockDbFixture.Context.FOBT.Count();
        _mockDbFixture.Context.FOBT.Should().Contain(result);
        Assert.True(initialCount < finalCount);
    }
       
    private static Core.Data.Entities.FOBT CreateFobt => new()
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
          
        CenseoId = "TestName1234",
        City = "Mysuru",
        DateOfBirth = DateTime.UtcNow,
          
        FirstName = "TestName",
        LastName = "H R",
        NationalProviderIdentifier = "1234567890",
        State = "karnataka",
        UserName = "TestName",
        ZipCode = "12345"
    };

    private static CreateOrUpdateFOBT CreateOrUpdateFobt => new()
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
            
        CenseoId = "TestName1234",
        City = "Mysuru",
        DateOfBirth = DateTime.UtcNow,
          
        FirstName = "TestName",
        LastName = "H R",
        NationalProviderIdentifier = "1234567890",
        State = "karnataka",
        UserName = "TestName",
        ZipCode = "12345"
    };
}