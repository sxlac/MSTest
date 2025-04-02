using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Messages.Commands;
using Signify.DEE.Svc.Core.Messages.Models;
using Signify.DEE.Svc.Core.Tests.Utilities;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using DeeNotPerformed = Signify.DEE.Svc.Core.Data.Entities.DeeNotPerformed;

namespace Signify.DEE.Svc.Core.Tests.Messages.Commands;

public class AddDeeNotPerformedHandlerTest : IClassFixture<EntityFixtures>
{
    private readonly IMapper _mapper;
    private readonly AddDeeNotPerformedHandler _addDeeNotPerformedHandler;
    private readonly DataContext _context;
    private static readonly FakeApplicationTime ApplicationTime = new();

    public AddDeeNotPerformedHandlerTest()
    {
        var options = new DbContextOptionsBuilder<DataContext>().UseInMemoryDatabase(databaseName: "DEE").Options;
        _mapper = A.Fake<IMapper>();
        _context = new DataContext(options);
        var logger = A.Fake<ILogger<AddDeeNotPerformedHandler>>();
        _addDeeNotPerformedHandler = new AddDeeNotPerformedHandler(logger, _mapper, _context);
    }

    [Fact]
    public async Task Should_Create_DeeNotPerformed()
    {
        A.CallTo(() => _mapper.Map<DeeNotPerformed>(A<AddDeeNotPerformed>._)).Returns(DeeNotPerformed);
        var initialCount = _context.DeeNotPerformed.Count();
        var result = await _addDeeNotPerformedHandler.Handle(AddDeeNotPerformed, CancellationToken.None);
        result.ExamId.Should().Be(AddDeeNotPerformed.ExamModel.ExamId);
        result.DeeNotPerformedId.Should().BeGreaterThan(0);
        result.ExamId.Should().Be(AddDeeNotPerformed.ExamModel.ExamId);
        _context.DeeNotPerformed.Count().Should().BeGreaterThanOrEqualTo(initialCount, "There shd be an insert");
    }

    [Fact]
    public async Task Should_Return_DeeNotPerformed()
    {
        A.CallTo(() => _mapper.Map<DeeNotPerformed>(A<AddDeeNotPerformed>._)).Returns(DeeNotPerformed);
        _context.DeeNotPerformed.Add(DeeNotPerformed);
        await _context.SaveChangesAsync();
        var result = await _addDeeNotPerformedHandler.Handle(AddDeeNotPerformed, CancellationToken.None);
        result.ExamId.Should().Be(AddDeeNotPerformed.ExamModel.ExamId);
        result.DeeNotPerformedId.Should().BeGreaterThan(0);
        _context.DeeNotPerformed.Any(x => x.ExamId == result.ExamId).Should().BeTrue();
    }


    private static ExamModel Model => new()
    {
        ExamId = 2121,
        ClientId = 14,
        DateOfService = ApplicationTime.UtcNow(),
        EvaluationId = 324359,
        MemberPlanId = 21074285,
        ProviderId = 42879,
        State = "Texas",
    };

    private static NotPerformedModel NotPerformedModel => new()
    {
        NotPerformedReasonId = 3131,
        AnswerId = 4141,
        Reason = "Patient Unwilling",
        ReasonType = "Member Refused",
        ReasonNotes = "Performed in recent past"
    };

    private static DeeNotPerformed DeeNotPerformed => new()
    {
        NotPerformedReasonId = 3131,
        ExamId = 2121,
    };

    private static AddDeeNotPerformed AddDeeNotPerformed => new()
    {
        ExamModel = Model,
        NotPerformedModel = NotPerformedModel
    };
}