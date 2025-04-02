using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using Signify.CKD.Svc.Core.Commands;
using Signify.CKD.Svc.Core.Tests.Utilities;
using Xunit;

namespace Signify.CKD.Svc.Core.Tests.Commands;

public class CreateOrUpdateCKDHandlerTest : IClassFixture<EntityFixtures>, IClassFixture<MockDbFixture>
{
    private readonly IMapper _mapper;
    private readonly CreateOrUpdateCKDHandler _createOrUpdateCkdHandler;
    private readonly MockDbFixture _mockDbFixture;

    public CreateOrUpdateCKDHandlerTest(MockDbFixture mockDbFixture)
    {
        _mapper = A.Fake<IMapper>();
        _mockDbFixture = mockDbFixture;
        _createOrUpdateCkdHandler = new CreateOrUpdateCKDHandler(mockDbFixture.Context, _mapper);
    }

    [Fact]
    public async Task Should_Create_CKD_DataCheck()
    {
        A.CallTo(() => _mapper.Map<Core.Data.Entities.CKD>(A<CreateOrUpdateCKD>._)).Returns(_createCkd);
        var ckd = await _createOrUpdateCkdHandler.Handle(_createCreateOrUpdateCkd, CancellationToken.None);
        _mockDbFixture.Context.CKD.ToList().Exists(x => x.AppointmentId == ckd.AppointmentId).Should().BeTrue();
    }
    [Fact]
    public async Task Should_Create_CKD_CountTest()
    {
        A.CallTo(() => _mapper.Map<Core.Data.Entities.CKD>(A<CreateOrUpdateCKD>._)).Returns(_createCkd);
        var initialCount = _mockDbFixture.Context.CKD.Count();
        await _createOrUpdateCkdHandler.Handle(_createCreateOrUpdateCkd, CancellationToken.None);
        _mockDbFixture.Context.CKD.Count().Should().BeGreaterThan(initialCount, "There shd be an insert");
    }
    [Fact]
    public async Task Should_Create_CKD()
    {
        A.CallTo(() => _mapper.Map<Core.Data.Entities.CKD>(A<CreateOrUpdateCKD>._)).Returns(_createCkd);
        var initialCount = _mockDbFixture.Context.CKD.Count();
        var ckd = await _createOrUpdateCkdHandler.Handle(_createCreateOrUpdateCkd, CancellationToken.None);
        ckd.CKDId.Should().BeGreaterThan(initialCount);
    }

    [Fact]
    public async Task Should_Create_CKD_TypeCheck()
    {
        A.CallTo(() => _mapper.Map<Core.Data.Entities.CKD>(A<CreateOrUpdateCKD>._)).Returns(_createCkd);
        var ckd = await _createOrUpdateCkdHandler.Handle(_createCreateOrUpdateCkd, CancellationToken.None);
        ckd.Should().BeOfType<Core.Data.Entities.CKD>();
    }

    [Fact]
    public async Task Should_Update_CKD_DataCheck()
    {
        A.CallTo(() => _mapper.Map<Core.Data.Entities.CKD>(A<CreateOrUpdateCKD>._)).Returns(_createCkd);
        var ckd = await _createOrUpdateCkdHandler.Handle(_createCreateOrUpdateCkd, CancellationToken.None);
        _mockDbFixture.Context.CKD.Should().Contain(ckd);
    }
   
    private readonly Core.Data.Entities.CKD _createCkd = new Core.Data.Entities.CKD
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
        CKDAnswer = "Albumin 80 - Creatinine 0 1",
        CenseoId = "ADarsh1234",
        City = "Mysuru",
        DateOfBirth = DateTime.UtcNow,
        ExpirationDate = DateTime.UtcNow,
        FirstName = "Adarsh",
        LastName = "H R",
        NationalProviderIdentifier = "1234567890",
        State = "karnataka",
        UserName = "ADarsh",
        ZipCode = "12345"
    };
    private readonly CreateOrUpdateCKD _createCreateOrUpdateCkd = new CreateOrUpdateCKD
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
        CKDAnswer = "Albumin 80 - Creatinine 0 1",
        CenseoId = "ADarsh1234",
        City = "Mysuru",
        DateOfBirth = DateTime.UtcNow,
        ExpirationDate = DateTime.UtcNow,
        FirstName = "Adarsh",
        LastName = "H R",
        NationalProviderIdentifier = "1234567890",
        State = "karnataka",
        UserName = "ADarsh",
        ZipCode = "12345"
    };
}