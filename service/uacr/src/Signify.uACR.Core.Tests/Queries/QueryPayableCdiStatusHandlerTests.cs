using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.uACR.Core.Data.Entities;
using Signify.uACR.Core.Queries;
using System.Linq;
using System.Threading.Tasks;
using System;
using Xunit;

namespace Signify.uACR.Core.Tests.Queries;

public class QueryPayableCdiStatusHandlerTests
{
    [Fact]
    public async Task Handle_WhenEntityExists_ReturnsEntity()
    {
        const int evaluationId = 1;

        await using var fixture = new MockDbFixture();
        
        var exam = new Exam
        {
            ExamId = 1,
            EvaluationId = 1,
            ApplicationId = "uACR"
        };
        
        exam = (await fixture.SharedDbContext.Exams.AddAsync(exam)).Entity;
        await fixture.SharedDbContext.SaveChangesAsync();

        var examStatuses = new ExamStatus
        {
            ExamStatusId = evaluationId,
            ExamId = 1,
            ExamStatusCodeId = 1,
            StatusDateTime = DateTimeOffset.Now,
            CreatedDateTime = DateTimeOffset.Now,
            ExamStatusCode = ExamStatusCode.CdiPassedReceived,
            Exam = exam
        };

        await fixture.SharedDbContext.ExamStatuses.AddAsync(examStatuses);
        await fixture.SharedDbContext.SaveChangesAsync();

        var subject = new QueryPayableCdiStatusHandler(A.Dummy<ILogger<QueryPayableCdiStatusHandler>>(), fixture.SharedDbContext);

        var actual = await subject.Handle(new QueryPayableCdiStatus{EvaluationId = 1}, default);

        Assert.NotNull(actual);
        Assert.Equal(1, actual.Exam.EvaluationId);
        Assert.Equal(ExamStatusCode.CdiPassedReceived.ExamStatusCodeId, actual.Exam.ExamStatuses.First().ExamStatusCodeId);
    }
    
    [Fact]
    public async Task Handle_WhenExamStatusCodeDoesntMatch_ReturnsNull()
    {
        const int evaluationId = 1;

        await using var fixture = new MockDbFixture();
        
        var exam = new Exam
        {
            ExamId = 1,
            EvaluationId = 1,
            ApplicationId = "uACR"
        };
        
        exam = (await fixture.SharedDbContext.Exams.AddAsync(exam)).Entity;
        await fixture.SharedDbContext.SaveChangesAsync();

        var examStatuses = new ExamStatus
        {
            ExamStatusId = evaluationId,
            ExamId = 1,
            ExamStatusCodeId = 1,
            StatusDateTime = DateTimeOffset.Now,
            CreatedDateTime = DateTimeOffset.Now,
            ExamStatusCode = ExamStatusCode.ExamPerformed,
            Exam = exam
        };

        await fixture.SharedDbContext.ExamStatuses.AddAsync(examStatuses);
        await fixture.SharedDbContext.SaveChangesAsync();

        var subject = new QueryPayableCdiStatusHandler(A.Dummy<ILogger<QueryPayableCdiStatusHandler>>(), fixture.SharedDbContext);

        var actual = await subject.Handle(new QueryPayableCdiStatus{EvaluationId = 1}, default);

        Assert.Null(actual);
    }
}