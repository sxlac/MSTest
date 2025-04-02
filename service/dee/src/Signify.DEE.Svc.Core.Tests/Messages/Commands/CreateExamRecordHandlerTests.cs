using AutoMapper;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Infrastructure;
using Signify.DEE.Svc.Core.Messages.Commands;
using Signify.DEE.Svc.Core.Messages.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using Signify.Dps.Observability.Library.Services;
using Xunit;
using Signify.DEE.Svc.Core.Data;

namespace Signify.DEE.Svc.Core.Tests.Messages.Commands;

public class CreateExamRecordHandlerTests
{
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly IApplicationTime _applicationTime = new FakeApplicationTime();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();
    private readonly DataContext _context;
    private readonly CreateExamRecordHandler _handler;

    public CreateExamRecordHandlerTests()
    {
        var options = new DbContextOptionsBuilder<DataContext>().UseInMemoryDatabase(databaseName: "DEE_CREATE_EXAM_RECORD_HANDLER_TEST").Options;
        _context = new DataContext(options);
        _handler = new CreateExamRecordHandler(A.Dummy<ILogger<CreateExamRecordHandler>>(),
            _mapper,
            _context,
            _applicationTime,
            _publishObservability);
    }

    [Fact]
    public async Task Handle_WhenExamExists_DoesNotInsertNewExam()
    {
        // Arrange
        var oldDos = DateTimeOffset.UtcNow;
        var newDos = oldDos.AddDays(3);

        var existing = new Exam
        {
            EvaluationId = 9,
            DateOfService = oldDos
        };


        await _context.Exams.AddAsync(existing);
        await _context.SaveChangesAsync();

        var initialExamCount = await _context.Exams.CountAsync();

        A.CallTo(() => _mapper.Map<ExamModel>(A<Exam>._))
            .ReturnsLazily(call =>
            {
                var exam = new ExamModel
                {
                    DateOfService = call.GetArgument<Exam>(0)!.DateOfService // All we care about
                };
                return exam;
            });

        var request = new CreateExamRecord
        {
            EvaluationId = existing.EvaluationId,
            ProviderId = "1",
            DateOfService = newDos
        };

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        Assert.Equal(initialExamCount, await _context.Exams.CountAsync());
        Assert.False(result.IsNew);
        Assert.NotNull(result.Exam);
        Assert.Equal(newDos, result.Exam.DateOfService);
    }

    [Fact]
    public async Task Handle_ShouldSaveRetinalImageTestingNotes()
    {
        // Arrange
        const string retinalImageTestingNotes = "Some Retinal Testing Notes";
        var request = new CreateExamRecord
        {
            EvaluationId = 123,
            State = "TN",
            EvaluationObjective = new EvaluationObjective(),
            ProviderId = "1",
            DateOfService = DateTimeOffset.UtcNow,
            RetinalImageTestingNotes = retinalImageTestingNotes
        };       

        // Act
        await _handler.Handle(request, CancellationToken.None);

        // Assert
        var exam = await _context.Exams.FirstOrDefaultAsync(e => e.EvaluationId == request.EvaluationId);
        Assert.NotNull(exam);
        Assert.Equal(retinalImageTestingNotes, exam.RetinalImageTestingNotes);
    }
    
    [Fact]
    public async Task Handle_ShouldSaveHasEnucleationWhenTrue()
    {
        // Arrange
        const bool hasEnucleation = true;
        var request = new CreateExamRecord
        {
            EvaluationId = 124,
            State = "TN",
            EvaluationObjective = new EvaluationObjective(),
            ProviderId = "1",
            DateOfService = DateTimeOffset.UtcNow,
            HasEnucleation = hasEnucleation
        };
        

        // Act
        await _handler.Handle(request, CancellationToken.None);

        // Assert
        var exam = await _context.Exams.FirstOrDefaultAsync(e => e.EvaluationId == request.EvaluationId);
        Assert.NotNull(exam);
        Assert.Equal(hasEnucleation, exam.HasEnucleation);
    }
    
    
    [Fact]
    public async Task Handle_ShouldSaveHasEnucleationWhenNull()
    {
        // Arrange
        var request = new CreateExamRecord
        {
            EvaluationId = 125,
            State = "TN",
            EvaluationObjective = new EvaluationObjective(),
            ProviderId = "1",
            DateOfService = DateTimeOffset.UtcNow,
            HasEnucleation = null
        };       

        // Act
        await _handler.Handle(request, CancellationToken.None);

        // Assert
        var exam = await _context.Exams.FirstOrDefaultAsync(e => e.EvaluationId == request.EvaluationId);
        Assert.NotNull(exam);
        Assert.Null(exam.HasEnucleation);
    }
}
