using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Signify.DEE.Svc.Core.ApiClient.Responses;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Messages.Models;
using Signify.DEE.Svc.Core.Messages.Queries;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Messages.Queries;

public class GetNotPerformedModelTest
{
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private DataContext ctx { get; set; }
    private static bool isDbPopulated = false;

    public GetNotPerformedModelTest()
    {
        var opt = new DbContextOptionsBuilder<DataContext>().UseInMemoryDatabase("GetNotPerformedModel").Options;
        ctx = new DataContext(opt);
        if (!isDbPopulated)
            PopulateDb();
    }

    private void PopulateDb()
    {
        ctx.NotPerformedReason.Add(GetNotPerformedReason());
        ctx.NotPerformedReason.Add(GetNotPerformedReasonForNotes());
        ctx.NotPerformedReason.Add(GetNotPerformedUnableReasonOther());
        ctx.NotPerformedReason.Add(GetNotPerformedMemberRefusedReasonOther());
        ctx.SaveChanges();
        isDbPopulated = true;
    }

    /// <summary>
    /// Response type
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task Get_Matching_Not_Performed_Reason()
    {
        //Arrange
        var reason = GetNotPerformedReason();
        var subject = new GetNotPerformedModelHandler(ctx, A.Dummy<ILogger<GetNotPerformedModelHandler>>(), _mapper);
        A.CallTo(() => _mapper.Map<NotPerformedModel>(A<NotPerformedReason>._)).Returns(new NotPerformedModel { AnswerId = 21119, NotPerformedReasonId = 1 });

        //Act
        var actualResult = await subject.Handle(GetNotPerformedModel(), CancellationToken.None);

        //Assert
        actualResult.AnswerId.Should().Be(reason.AnswerId);
        actualResult.NotPerformedReasonId.Should().Be(reason.NotPerformedReasonId);
        actualResult.Should().NotBe(null);
    }

    /// <summary>
    /// Response type
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task Get_Matching_Not_Performed_ReasonType_And_ReasonNotes()
    {
        //Arrange
        var subject = new GetNotPerformedModelHandler(ctx, A.Dummy<ILogger<GetNotPerformedModelHandler>>(), _mapper);
        A.CallTo(() => _mapper.Map<NotPerformedModel>(A<NotPerformedReason>._)).Returns(new NotPerformedModel { AnswerId = 30952, NotPerformedReasonId = 2, ReasonNotes = "" });

        //Act
        var actualResult = await subject.Handle(GetNotPerformedModelWithNotes(), CancellationToken.None);

        //Assert
        actualResult.ReasonType.Should().NotBeEmpty();
        actualResult.ReasonNotes.Should().Be("Notes");
    }

    [Fact]
    public async Task WhenPriorityUnableToPerformNotesPresent_SetThoseAsReasonNotes()
    {
        //Arrange
        var subject = new GetNotPerformedModelHandler(ctx, A.Dummy<ILogger<GetNotPerformedModelHandler>>(), _mapper);
        A.CallTo(() => _mapper.Map<NotPerformedModel>(A<NotPerformedReason>._)).Returns(new NotPerformedModel { AnswerId = 52851, NotPerformedReasonId = 4, ReasonNotes = "Other" });

        //Act
        var actualResult = await subject.Handle(GetUnableToPerformWithPriorityNotes(), CancellationToken.None);

        //Assert
        actualResult.ReasonType.Should().NotBeEmpty();
        actualResult.ReasonNotes.Should().Be("PriortyUnableToPerformNotes-52852");
    }

    [Fact]
    public async Task WhenPriorityMemberRefusedNotesPresent_SetThoseAsReasonNotes()
    {
        //Arrange        
        var subject = new GetNotPerformedModelHandler(ctx, A.Dummy<ILogger<GetNotPerformedModelHandler>>(), _mapper);
        A.CallTo(() => _mapper.Map<NotPerformedModel>(A<NotPerformedReason>._)).Returns(new NotPerformedModel { AnswerId = 30947, NotPerformedReasonId = 3, ReasonNotes = "Other" });

        //Act
        var actualResult = await subject.Handle(GetMemberRefusedWithPriorityNotes(), CancellationToken.None);

        //Assert
        actualResult.ReasonType.Should().NotBeEmpty();
        actualResult.ReasonNotes.Should().Be("PriortyMemberRefusedNotes-52850");
    }

    [Fact]
    public async Task WhenPriorityUnableToPerformNotesAbsent_FallbackToOldNotes()
    {
        //Arrange        
        var subject = new GetNotPerformedModelHandler(ctx, A.Dummy<ILogger<GetNotPerformedModelHandler>>(), _mapper);
        A.CallTo(() => _mapper.Map<NotPerformedModel>(A<NotPerformedReason>._)).Returns(new NotPerformedModel { AnswerId = 52851, NotPerformedReasonId = 4, ReasonNotes = "Other" });

        //Act
        var actualResult = await subject.Handle(GetUnableToPerformWithoutNotesFallsBackToOldNotes(), CancellationToken.None);

        //Assert
        actualResult.ReasonType.Should().NotBeEmpty();
        actualResult.ReasonNotes.Should().Be("OldNotes-30955");
    }

    [Fact]
    public async Task WhenPriorityMemberRefusedNotesAbsent_FallbackToOldNotes()
    {
        //Arrange        
        var subject = new GetNotPerformedModelHandler(ctx, A.Dummy<ILogger<GetNotPerformedModelHandler>>(), _mapper);
        A.CallTo(() => _mapper.Map<NotPerformedModel>(A<NotPerformedReason>._)).Returns(new NotPerformedModel { AnswerId = 30947, NotPerformedReasonId = 3, ReasonNotes = "Other" });

        //Act
        var actualResult = await subject.Handle(GetMemberRefusedWithoutPriorityNotesFallsBackToOldNotes(), CancellationToken.None);

        //Assert
        actualResult.ReasonType.Should().NotBeEmpty();
        actualResult.ReasonNotes.Should().Be("OldNotes-30948");
    }

    private static NotPerformedReason GetNotPerformedReason() => new()
    {
        NotPerformedReasonId = 1,
        AnswerId = 21119,
        Reason = "Patient Unwilling"
    };

    private static NotPerformedReason GetNotPerformedReasonForNotes() => new()
    {
        NotPerformedReasonId = 2,
        AnswerId = 30952,
        Reason = "Equipment problem"
    };

    private static NotPerformedReason GetNotPerformedMemberRefusedReasonOther() => new()
    {
        NotPerformedReasonId = 3,
        AnswerId = 30947,
        Reason = "Other"
    };

    private static NotPerformedReason GetNotPerformedUnableReasonOther() => new()
    {
        NotPerformedReasonId = 4,
        AnswerId = 52851,
        Reason = "Other"
    };

    private static GetNotPerformedModel GetNotPerformedModel() => new()
    {
        EvaluationId = 12345,
        Answers =
        [
            new EvaluationAnswer {AnswerId = 21119},
            new EvaluationAnswer {AnswerId = 92821}
        ]
    };

    private static GetNotPerformedModel GetNotPerformedModelWithNotes() => new()
    {
        EvaluationId = 12345,
        Answers =
        [
            new EvaluationAnswer {AnswerId = 30952},
            new EvaluationAnswer {AnswerId = 30955, AnswerValue = "Notes"}
        ]
    };

    private static GetNotPerformedModel GetMemberRefusedWithPriorityNotes() => new()
    {
        EvaluationId = 12345,
        Answers =
        [
            new EvaluationAnswer {AnswerId = 30947},
            new EvaluationAnswer {AnswerId = 52850, AnswerValue = "PriortyMemberRefusedNotes-52850"}
        ]
    };

    private static GetNotPerformedModel GetMemberRefusedWithoutPriorityNotesFallsBackToOldNotes() => new()
    {
        EvaluationId = 12345,
        Answers =
        [
            new EvaluationAnswer {AnswerId = 30947},
            new EvaluationAnswer {AnswerId = 30948, AnswerValue = "OldNotes-30948"}
        ]
    };

    private static GetNotPerformedModel GetUnableToPerformWithPriorityNotes() => new()
    {
        EvaluationId = 12345,
        Answers =
        [
            new EvaluationAnswer {AnswerId = 52851},
            new EvaluationAnswer {AnswerId = 52852, AnswerValue = "PriortyUnableToPerformNotes-52852"}
        ]
    };

    private static GetNotPerformedModel GetUnableToPerformWithoutNotesFallsBackToOldNotes() => new()
    {
        EvaluationId = 12345,
        Answers =
    [
        new EvaluationAnswer {AnswerId = 52851},
        new EvaluationAnswer {AnswerId = 30955, AnswerValue = "OldNotes-30955"}
    ]
    };
}