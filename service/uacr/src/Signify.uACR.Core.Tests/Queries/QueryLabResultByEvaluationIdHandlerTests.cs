using System;
using System.Threading.Tasks;
using Signify.uACR.Core.Data.Entities;
using Signify.uACR.Core.Queries;
using Xunit;

namespace Signify.uACR.Core.Tests.Queries;

public class QueryLabResultByEvaluationIdHandlerTests
{
    [Fact]
    public async Task Handle_WhenEntityDoesNotExist_ReturnsNull()
    {
        //Arrange
        const int evaluationId = 12345;
        await using var fixture = new MockDbFixture();
        var subject = new QueryLabResultByEvaluationIdHandler(fixture.SharedDbContext);
        
        //Arrange
        var actual = await subject.Handle(new QueryLabResultByEvaluationId(evaluationId), default);

        //Assert
        Assert.Null(actual);
    }

    [Fact]
    public async Task Handle_WhenEntityExists_ReturnsEntity()
    {
        //Arrange
        const int evaluationId = 12345;
        var receivedDate = new DateTimeOffset(DateTime.UtcNow);
        await using var fixture = new MockDbFixture();
        var labResult = new LabResult
        {
            EvaluationId = evaluationId,
            ReceivedDate = receivedDate,
            Normality = "Normal",
            NormalityCode = "N"
        };
        var exam = new Exam
        {
            EvaluationId = evaluationId,
            ApplicationId = nameof(ApplicationId),
            DateOfService = receivedDate
        };
        await fixture.SharedDbContext.Exams.AddAsync(exam);
        await fixture.SharedDbContext.LabResults.AddAsync(labResult);
        await fixture.SharedDbContext.SaveChangesAsync();
        
        //Act
        var subject = new QueryLabResultByEvaluationIdHandler(fixture.SharedDbContext);
        var actual = await subject.Handle(new QueryLabResultByEvaluationId(evaluationId), default);

        //Assert
        Assert.NotNull(actual);
    }
    
    [Fact]
    public async Task Handle_WhenEntityExists_DateTimeOffset_ReturnsEntity()
    {
        //Arrange
        const int evaluationId = 12345;
        var receivedDate = new DateTimeOffset(2023, 8, 15, 00, 30, 32, 545, new TimeSpan(0, 0, 0));

        await using var fixture = new MockDbFixture();
        var labResult = new LabResult
        {
            EvaluationId = evaluationId,
            ReceivedDate = receivedDate,
            Normality = "Normal",
            NormalityCode = "N"
        };
        var exam = new Exam
        {
            EvaluationId = evaluationId,
            ApplicationId = nameof(ApplicationId),
            DateOfService = receivedDate
        };
        await fixture.SharedDbContext.Exams.AddAsync(exam);
        await fixture.SharedDbContext.LabResults.AddAsync(labResult);
        await fixture.SharedDbContext.SaveChangesAsync();
        
        //Act
        var subject = new QueryLabResultByEvaluationIdHandler(fixture.SharedDbContext);
        var actual = await subject.Handle(new QueryLabResultByEvaluationId(evaluationId), default);

        //Assert
        Assert.NotNull(actual);
    }
}