using Signify.PAD.Svc.Core.Queries;
using Signify.PAD.Svc.Core.Tests.Utilities;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Queries;

public class GetPadByMemberPlanIdTests : IClassFixture<MockDbFixture>
{
    private readonly GetPadByMemberPlanIdHandler _subject;

    private static readonly Core.Data.Entities.PAD Pad = new()
    {
        MemberPlanId = 1,
        DateOfService = DateTime.UtcNow
    };

    public GetPadByMemberPlanIdTests(MockDbFixture mockDbFixture)
    {
        _subject = new GetPadByMemberPlanIdHandler(mockDbFixture.Context);

        // This test class is instantiated for each test run, but the same fixture instance
        // is shared among all test runs
        if (mockDbFixture.Context.PAD.Local.Contains(Pad)) return;
        mockDbFixture.Context.PAD.Add(Pad);
        mockDbFixture.Context.SaveChanges();
    }

    [Theory]
    [MemberData(nameof(Handle_TestData))]
    public async Task Handle_Tests(GetPadByMemberPlanId request, bool expectResult)
    {
        var result = await _subject.Handle(request, default);

        if (expectResult)
            Assert.NotNull(result);
        else
            Assert.Null(result);
    }

    public static IEnumerable<object[]> Handle_TestData()
    {
        yield return
        [
            new GetPadByMemberPlanId(Pad.MemberPlanId!.Value, Pad.DateOfService!.Value),
            true
        ];
        yield return
        [
            new GetPadByMemberPlanId(Pad.MemberPlanId!.Value, Pad.DateOfService!.Value.Date),
            true
        ];
        yield return
        [
            new GetPadByMemberPlanId(Pad.MemberPlanId!.Value, Pad.DateOfService!.Value.Date.AddHours(1)),
            true
        ];
        yield return
        [
            new GetPadByMemberPlanId(Pad.MemberPlanId!.Value, Pad.DateOfService!.Value.AddDays(1)),
            false
        ];
        yield return
        [
            new GetPadByMemberPlanId(Pad.MemberPlanId!.Value + 1, Pad.DateOfService!.Value),
            false
        ];
    }
}