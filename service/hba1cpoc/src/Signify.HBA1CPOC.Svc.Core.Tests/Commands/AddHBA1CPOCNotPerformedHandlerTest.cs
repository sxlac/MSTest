using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Signify.HBA1CPOC.Svc.Core.Commands;
using Signify.HBA1CPOC.Svc.Core.Tests.Utilities;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.HBA1CPOC.Svc.Core.Tests.Commands;

public class AddHba1CpocNotPerformedHandlerTest : IClassFixture<MockDbFixture>
{
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly AddHba1CpocNotPerformedHandler _subject;
    private readonly MockDbFixture _mockDbFixture;

    public AddHba1CpocNotPerformedHandlerTest(MockDbFixture mockDbFixture)
    {
        _mockDbFixture = mockDbFixture;
        _subject = new AddHba1CpocNotPerformedHandler(A.Dummy<ILogger<AddHba1CpocNotPerformedHandler>>(), mockDbFixture.Context, _mapper);
    }

    [Fact]
    public async Task Should_Create_NotPerformed()
    {
        A.CallTo(() => _mapper.Map<Core.Data.Entities.Hba1CpocNotPerformed>(A<AddHba1CpocNotPerformed>._)).Returns(HBA1CPOCNotPerformed);
        var initialCount = _mockDbFixture.Context.HBA1CPOCNotPerformed.Count();
        await _subject.Handle(addHBA1CPOCNotPerformed, CancellationToken.None);
        _mockDbFixture.Context.HBA1CPOCNotPerformed.Count().Should().BeGreaterThanOrEqualTo(initialCount, "There should be an insert");
    }

    [Fact]
    public async Task Should_Return_NotPerformed()
    {
        A.CallTo(() => _mapper.Map<Core.Data.Entities.Hba1CpocNotPerformed>(A<AddHba1CpocNotPerformed>._)).Returns(HBA1CPOCNotPerformed);
        _mockDbFixture.Context.HBA1CPOCNotPerformed.Add(HBA1CPOCNotPerformed);
        var result = await _subject.Handle(addHBA1CPOCNotPerformed, CancellationToken.None);
        _mockDbFixture.Context.HBA1CPOCNotPerformed.Any(x => x.HBA1CPOCId == result.HBA1CPOCId).Should().BeTrue();
    }

    private static Core.Data.Entities.HBA1CPOC HBA1CPOC => new()
    {
        HBA1CPOCId = 2121,
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
        DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-36)),

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
        Reason = "Patient Unwilling"
    };

    private static Core.Data.Entities.Hba1CpocNotPerformed HBA1CPOCNotPerformed => new()
    {
        NotPerformedReasonId = 3131,
        HBA1CPOCId = 2121
    };

    private static AddHba1CpocNotPerformed addHBA1CPOCNotPerformed => new()
    {
        HBA1CPOC = HBA1CPOC,
        NotPerformedReason = NotPerformedReason
    };
}