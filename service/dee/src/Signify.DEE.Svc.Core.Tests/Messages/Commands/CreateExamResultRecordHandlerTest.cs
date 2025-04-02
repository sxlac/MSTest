using AutoMapper;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Messages.Commands;
using Signify.DEE.Svc.Core.Messages.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Messages.Commands;

public class CreateResultRecordHandlerTest
{
    private readonly ILogger<CreateResultRecordCommandHandler> _log;
    private readonly IMapper _mapper;
    private readonly DataContext _context;
    private readonly DataContext _contextInMemory;
    private readonly FakeApplicationTime _applicationTime = new();

    public CreateResultRecordHandlerTest()
    {
        var options = new DbContextOptionsBuilder<DataContext>().UseInMemoryDatabase(databaseName: "DEECreateExamResultRecordHandlerTests").Options;
        _log = A.Fake<ILogger<CreateResultRecordCommandHandler>>();
        _mapper = A.Fake<IMapper>();
        _context = A.Fake<DataContext>();
        _contextInMemory = new DataContext(options);
    }

    [Fact]
    public Task Exam_With_ExamResult()
    {
        // Arrange
        var exam = new Exam { CreatedDateTime = _applicationTime.LocalNow(), Gradeable = true, ExamId = 1, EvaluationId = 12345 };
        ExamResult.ExamFindings.Add(ExamFinding);
        var fakeDbSet = FakeDbSet(exam);
        fakeDbSet.First().ExamResults.Add(ExamResult);

        A.CallTo(() => _context.Exams).Returns(fakeDbSet);
        A.CallTo(() => _mapper.Map<ExamResult>(A<ExamResultModel>._)).Returns(ExamResult);

        var request = new CreateExamResultRecord(ExamResultModel);
        var handler = new CreateResultRecordCommandHandler(_log, _mapper, _context);

        // Act
        var result = Record.ExceptionAsync(() => handler.Handle(request, CancellationToken.None));

        //Assert
        Assert.Null(result.Exception);
        return Task.FromResult(Task.CompletedTask);
    }

    [Fact]
    public Task Exam_With_ExamResult_LeftEyeHasPathology_False()
    {
        // Arrange
        var exam = new Exam { CreatedDateTime = _applicationTime.LocalNow(), Gradeable = true, ExamId = 2, EvaluationId = 12346 };
        var modifiedExamResult = new ExamResult
        {
            CarePlan = "Re-scan the patient in 12 months or in the next calendar year.",
            DateSigned = _applicationTime.UtcNow(),
            ExamId = 2,
            ExamResultId = 2832,
            GradableImage = true,
            ExamDiagnoses = new List<ExamDiagnosis>
            {
                new ExamDiagnosis
                {
                    ExamDiagnosisId = 2741,
                    ExamResultId = 2832,
                    Diagnosis = "EB21233"
                }
            },
            ExamFindings = new List<ExamFinding>
            {
                new ExamFinding
                {
                    ExamFindingId = 4957,
                    ExamResultId = 2832,
                    Finding = "Macular Edema - None"
                }
            },
            LeftEyeHasPathology = false,
            RightEyeHasPathology = true            
        };

        _contextInMemory.Exams.Add(exam);
        _contextInMemory.SaveChanges();

        A.CallTo(() => _mapper.Map<ExamResult>(A<ExamResultModel>._)).Returns(modifiedExamResult);

        var request = new CreateExamResultRecord(ExamResultModelWithoutLeft);
        var handler = new CreateResultRecordCommandHandler(_log, _mapper, _contextInMemory);

        // Act
        var result = Record.ExceptionAsync(() => handler.Handle(request, CancellationToken.None));

        // Assert
        Assert.Null(result.Exception);        
        Assert.Equal("N", modifiedExamResult.NormalityIndicator);
        return Task.FromResult(Task.CompletedTask);
    }

    [Fact]
    public Task Exam_With_ExamResult_LeftEyeNotGradable_ResultIsUndetermined()
    {
        // Arrange
        var exam = new Exam { CreatedDateTime = _applicationTime.LocalNow(), Gradeable = true, ExamId = 4, EvaluationId = 123467 };
        var modifiedExamResult = new ExamResult
        {
            CarePlan = "Re-scan the patient in 12 months or in the next calendar year.",
            DateSigned = _applicationTime.UtcNow(),
            ExamId = 4,
            ExamResultId = 2832,
            GradableImage = true,
            ExamDiagnoses = new List<ExamDiagnosis>
            {
                new ExamDiagnosis
                {
                    ExamDiagnosisId = 2741,
                    ExamResultId = 2832,
                    Diagnosis = "EB21233"
                }
            },            
            LeftEyeHasPathology = false,
            RightEyeHasPathology = false,           
        };

        _contextInMemory.Exams.Add(exam);
        _contextInMemory.SaveChanges();

        A.CallTo(() => _mapper.Map<ExamResult>(A<ExamResultModel>._)).Returns(modifiedExamResult);

        var request = new CreateExamResultRecord(ExamResultModelLeftUngradable);
        var handler = new CreateResultRecordCommandHandler(_log, _mapper, _contextInMemory);

        // Act
        var result = Record.ExceptionAsync(() => handler.Handle(request, CancellationToken.None));

        // Assert
        Assert.Null(result.Exception);
        Assert.Equal("U", modifiedExamResult.NormalityIndicator);
        return Task.FromResult(Task.CompletedTask);
    }

    [Fact]
    public Task Exam_With_ExamResult_RightEyeHasPathology_True()
    {
        // Arrange
        var exam = new Exam { CreatedDateTime = _applicationTime.LocalNow(), Gradeable = true, ExamId = 3, EvaluationId = 12346 };
        var modifiedExamResult = new ExamResult
        {
            CarePlan = "Re-scan the patient in 12 months or in the next calendar year.",
            DateSigned = _applicationTime.UtcNow(),
            ExamId = 3,
            ExamResultId = 2833,
            GradableImage = true,
            ExamDiagnoses = new List<ExamDiagnosis>
            {
                new ExamDiagnosis
                {
                    ExamDiagnosisId = 2741,
                    ExamResultId = 2833,
                    Diagnosis = "EB21233"
                }
            },
            ExamFindings = new List<ExamFinding>
            {
                new ExamFinding
                {
                    ExamFindingId = 4957,
                    ExamResultId = 2833,
                    Finding = "Diabetic Retinopathy - Mild",
                    NormalityIndicator = "A"
                }
            },
            LeftEyeHasPathology = false,
            RightEyeHasPathology = true,
        };

        _contextInMemory.Exams.Add(exam);
        _contextInMemory.SaveChanges();

        A.CallTo(() => _mapper.Map<ExamResult>(A<ExamResultModel>._)).Returns(modifiedExamResult);

        var request = new CreateExamResultRecord(ExamResultModelWithRight);
        var handler = new CreateResultRecordCommandHandler(_log, _mapper, _contextInMemory);

        // Act
        var result = Record.ExceptionAsync(() => handler.Handle(request, CancellationToken.None));

        // Assert
        Assert.Null(result.Exception);
        Assert.Equal("A", modifiedExamResult.NormalityIndicator);
        return Task.FromResult(Task.CompletedTask);
    }


    [Fact]
    public Task Exam_With_No_Evaluation()
    {
        // Arrange
        var exam = new Exam { CreatedDateTime = _applicationTime.UtcNow(), Gradeable = true, ExamId = 1 };
        var fakeDbSet = FakeDbSet(exam);

        A.CallTo(() => _context.Exams).Returns(fakeDbSet);
        A.CallTo(() => _mapper.Map<ExamResult>(A<ExamResultModel>._)).Returns(ExamResult);

        var request = new CreateExamResultRecord(ExamResultModel);
        var handler = new CreateResultRecordCommandHandler(_log, _mapper, _context);

        // Act
        var result = Record.ExceptionAsync(() => handler.Handle(request, CancellationToken.None));

        //Assert
        Assert.Null(result.Exception);
        return Task.FromResult(Task.FromResult(Task.CompletedTask));
    }

    [Fact]
    public async Task Exam_With_No_Result()
    {
        // Arrange
        var exam = new Exam { CreatedDateTime = _applicationTime.UtcNow(), Gradeable = true, ExamId = 4436, EvaluationId = 61252 };
        _contextInMemory.Exams.Add(exam);
        await _contextInMemory.SaveChangesAsync();

        A.CallTo(() => _mapper.Map<ExamResult>(A<ExamResultModel>._)).Returns(ExamResult);

        var request = new CreateExamResultRecord(ExamResultModel);
        var handler = new CreateResultRecordCommandHandler(_log, _mapper, _context);

        // Act
        var result = Record.ExceptionAsync(() => handler.Handle(request, CancellationToken.None));

        //Assert
        Assert.Null(result.Exception);
    }

    private static List<string> LeftEyeFindings => ["Macular Edema - None", "Diabetic Retinopathy - Mild"];

    private static List<string> RightEyeFindings =>
        ["Macular Edema - None", "Diabetic Retinopathy - None", "Other - Cataract"];

    private static List<string> Diagnosis => ["E73221", "E73223"];

    private ExamResultModel ExamResultModel => new()
    {
        CarePlan = "Humana",
        DateSigned = _applicationTime.UtcNow(),
        ExamId = 4436,
        ExamResultId = 2831,
        GradableImage = true,
        PatientId = 35125195,
        LeftEyeFindings = LeftEyeFindings,
        RightEyeFindings = RightEyeFindings,
        Diagnoses = Diagnosis,
        LeftEyeHasPathology = true,
        RightEyeHasPathology = true,
    };

    private ExamResult ExamResult => new()
    {
        CarePlan = "Re-scan the patient in 12 months or in the next calendar year.",
        DateSigned = _applicationTime.UtcNow(),
        ExamId = 4436,
        ExamResultId = 2831,
        GradableImage = true,
        ExamDiagnoses = new List<ExamDiagnosis> { ExamDiagnosis },
        ExamFindings = new List<ExamFinding> { ExamFinding },
        LeftEyeHasPathology = true,
        RightEyeHasPathology = true,
    };

    private static ExamFinding ExamFinding => new()
    {
        ExamFindingId = 4957,
        ExamResultId = 2831,
        Finding = "Macular Edema - None",
    };

    private static ExamDiagnosis ExamDiagnosis => new()
    {
        ExamDiagnosisId = 2741,
        ExamResultId = 2831,
        Diagnosis = "EB21233",
    };

    private ExamResultModel ExamResultModelWithoutLeft => new()
    {
        CarePlan = "Humana",
        DateSigned = _applicationTime.UtcNow(),
        ExamId = 2,
        ExamResultId = 2832,
        GradableImage = true,
        PatientId = 35125195,
        RightEyeFindings = RightEyeFindings,
        Diagnoses = Diagnosis,
        LeftEyeHasPathology = false,
        RightEyeHasPathology = true,
        LeftEyeGradable = true,
        RightEyeGradable = true
    };

    private ExamResultModel ExamResultModelLeftUngradable => new()
    {
        CarePlan = "Humana",
        DateSigned = _applicationTime.UtcNow(),
        ExamId = 4,
        ExamResultId = 2832,
        GradableImage = true,
        PatientId = 35125195,
        RightEyeFindings = RightEyeFindings,
        Diagnoses = Diagnosis,
        LeftEyeHasPathology = false,
        RightEyeHasPathology = false,
        LeftEyeGradable = false,
        RightEyeGradable = true
    };

    private ExamResultModel ExamResultModelWithRight => new()
    {
        CarePlan = "Humana",
        DateSigned = _applicationTime.UtcNow(),
        ExamId = 3,
        ExamResultId = 2833,
        GradableImage = true,
        PatientId = 35125195,
        RightEyeFindings = RightEyeFindings,
        Diagnoses = Diagnosis,
        LeftEyeHasPathology = false,
        RightEyeHasPathology = true,
        LeftEyeGradable = true,
        RightEyeGradable = true
    };

    private static DbSet<Exam> FakeDbSet(Exam exams)
    {
        var fakeIQueryable = new List<Exam> { exams }.AsQueryable();
        var fakeDbSet = A.Fake<DbSet<Exam>>((d => d.Implements(typeof(IQueryable<Exam>))));
        A.CallTo(() => ((IQueryable<Exam>)fakeDbSet).GetEnumerator()).Returns(fakeIQueryable.GetEnumerator());
        A.CallTo(() => ((IQueryable<Exam>)fakeDbSet).Provider).Returns(fakeIQueryable.Provider);
        A.CallTo(() => ((IQueryable<Exam>)fakeDbSet).Expression).Returns(fakeIQueryable.Expression);
        A.CallTo(() => ((IQueryable<Exam>)fakeDbSet).ElementType).Returns(fakeIQueryable.ElementType);
        return fakeDbSet;
    }
}