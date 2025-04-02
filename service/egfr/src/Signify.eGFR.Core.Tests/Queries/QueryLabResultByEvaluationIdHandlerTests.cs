using System;
using System.Threading.Tasks;
using Signify.eGFR.Core.Data.Entities;
using Signify.eGFR.Core.Queries;
using Xunit;

namespace Signify.eGFR.Core.Tests.Queries;

public class QueryLabResultByExamEvaluationIdHandlerTests
{
    [Fact]
    public async Task Handle_WhenEntityDoesNotExist_ReturnsNull()
    {
        //Arrange
        const int evaluationId = 1;
        await using var fixture = new MockDbFixture();
        var subject = new QueryQuestLabResultByEvaluationIdHandler(fixture.SharedDbContext);
        
        //Arrange
        var actual = await subject.Handle(new QueryQuestLabResultByEvaluationId(evaluationId), default);

        //Assert
        Assert.Null(actual);
    }

    [Fact]
    public async Task Handle_WhenEntityExists_ReturnsEntity()
    {
        //Arrange
        const int evaluationId = 1;
        const string censeoId = "abc";
        var collectionDate = new DateTimeOffset(DateTime.UtcNow);
        await using var fixture = new MockDbFixture();
        var labResult = new QuestLabResult
        {
            CenseoId = censeoId,
            CollectionDate = collectionDate
        };
        var exam = new Exam
        {
            EvaluationId = evaluationId,
            ApplicationId = nameof(ApplicationId),
            CenseoId = censeoId,
            DateOfService = collectionDate
        };
        await fixture.SharedDbContext.Exams.AddAsync(exam);
        await fixture.SharedDbContext.QuestLabResults.AddAsync(labResult);
        await fixture.SharedDbContext.SaveChangesAsync();
        
        //Act
        var subject = new QueryQuestLabResultByEvaluationIdHandler(fixture.SharedDbContext);
        var actual = await subject.Handle(new QueryQuestLabResultByEvaluationId(evaluationId), default);

        //Assert
        Assert.NotNull(actual);
    }

    [Fact]
    public async Task Handle_WhenEntityExists_DateTimeOffset_ReturnsEntity()
    {
        //Arrange
        const int evaluationId = 2;
        const string censeoId = "DateTimeOffset";
        var collectionDate = new DateTimeOffset(2023, 8, 15, 00, 30, 32, 545, new TimeSpan(0, 0, 0));
        await using var fixture = new MockDbFixture();
        var labResult = new QuestLabResult
        {
            CenseoId = censeoId,
            CollectionDate = collectionDate
        };
        var exam = new Exam
        {
            EvaluationId = evaluationId,
            ApplicationId = nameof(ApplicationId),
            CenseoId = censeoId,
            DateOfService = collectionDate
        };
        await fixture.SharedDbContext.Exams.AddAsync(exam);
        await fixture.SharedDbContext.QuestLabResults.AddAsync(labResult);
        await fixture.SharedDbContext.SaveChangesAsync();
        
        //Act
        var subject = new QueryQuestLabResultByEvaluationIdHandler(fixture.SharedDbContext);
        var actual = await subject.Handle(new QueryQuestLabResultByEvaluationId(evaluationId), default);

        //Assert
        Assert.NotNull(actual);
    }
}