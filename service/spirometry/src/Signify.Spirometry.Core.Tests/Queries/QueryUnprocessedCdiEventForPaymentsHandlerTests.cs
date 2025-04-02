using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Queries;
using System.Threading.Tasks;
using System;
using Xunit;
using StatusCode = Signify.Spirometry.Core.Models.StatusCode;

namespace Signify.Spirometry.Core.Tests.Queries;

public class QueryUnprocessedCdiEventForPaymentsHandlerTests
{
    [Fact]
    public async Task Handle_WhenEntityDoesNotExist_ReturnsEmptyList()
    {
        await using var fixture = new MockDbFixture();
        const long evaluationId = 12345;
        var subject = new QueryUnprocessedCdiEventForPaymentsHandler(fixture.SharedDbContext);

        var result = await subject.Handle(new QueryUnprocessedCdiEventForPayments(evaluationId), default);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_When_CdiEventsExist_But_ExamStatus_IsEmpty_Returns_All_CdiEvents()
    {
        await using var fixture = new MockDbFixture();
        const int evaluationId = 1;
        var exam = new SpirometryExam
        {
            EvaluationId = evaluationId
        };
        await fixture.SharedDbContext.SpirometryExams.AddAsync(exam);
        await fixture.SharedDbContext.SaveChangesAsync();
        var cdiEvent1 = new CdiEventForPayment
        {
            EvaluationId = evaluationId,
            RequestId = Guid.NewGuid(),
            DateTime = new FakeApplicationTime().UtcNow(),
            EventType = "CDIFailedEvent",
            PayProvider = false,
            Reason = "Test reason",
            ApplicationId = "DpsApp"
        };
        var cdiEvent2 = new CdiEventForPayment
        {
            EvaluationId = evaluationId,
            RequestId = Guid.NewGuid(),
            DateTime = new FakeApplicationTime().UtcNow(),
            EventType = "CDIPassedEvent",
            ApplicationId = "DpsApp"
        };
        await fixture.SharedDbContext.CdiEventForPayments.AddAsync(cdiEvent1);
        await fixture.SharedDbContext.CdiEventForPayments.AddAsync(cdiEvent2);
        await fixture.SharedDbContext.SaveChangesAsync();

        var subject = new QueryUnprocessedCdiEventForPaymentsHandler(fixture.SharedDbContext);

        var result = await subject.Handle(new QueryUnprocessedCdiEventForPayments(evaluationId), default);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }
    
    [Fact]
    public async Task Handle_When_CdiEvents_ContainsMoreItems_Than_ExamStatus_Returns_UnProcessed_CdiEvents()
    {
        await using var fixture = new MockDbFixture();
        const int evaluationId = 1;
        const int examId = 1;
        var exam = new SpirometryExam
        {
            EvaluationId = evaluationId,
            SpirometryExamId = examId
        };
        await fixture.SharedDbContext.SpirometryExams.AddAsync(exam);
        await fixture.SharedDbContext.SaveChangesAsync();
        var cdiEvent1 = new CdiEventForPayment
        {
            EvaluationId = evaluationId,
            RequestId = Guid.NewGuid(),
            DateTime = new FakeApplicationTime().UtcNow().AddDays(-2),
            EventType = "CDIFailedEvent",
            PayProvider = false,
            Reason = "Test reason",
            ApplicationId = "DpsApp"
        };
        var cdiEvent2 = new CdiEventForPayment
        {
            EvaluationId = evaluationId,
            RequestId = Guid.NewGuid(),
            DateTime = new FakeApplicationTime().UtcNow().AddDays(-1),
            EventType = "CDIPassedEvent",
            ApplicationId = "DpsApp"
        };
        await fixture.SharedDbContext.CdiEventForPayments.AddAsync(cdiEvent1);
        await fixture.SharedDbContext.CdiEventForPayments.AddAsync(cdiEvent2);
        await fixture.SharedDbContext.SaveChangesAsync();
        var examStatus = new ExamStatus
        {
            SpirometryExamId = examId,
            StatusCodeId = (int)StatusCode.CdiFailedWithoutPayReceived,
            StatusDateTime = cdiEvent1.DateTime.UtcDateTime
        };
        await fixture.SharedDbContext.ExamStatuses.AddAsync(examStatus);
        await fixture.SharedDbContext.SaveChangesAsync();

        var subject = new QueryUnprocessedCdiEventForPaymentsHandler(fixture.SharedDbContext);

        var result = await subject.Handle(new QueryUnprocessedCdiEventForPayments(evaluationId), default);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(cdiEvent2.EventType, result[0].EventType);
        Assert.Equal(cdiEvent2.DateTime, result[0].DateTime);
    }
    
    /// <summary>
    /// This is an invalid scenario. There should always be more items in CdiEvent table than cdi related items in ExamStatus table
    /// </summary>
    [Fact]
    public async Task Handle_When_CdiEvents_ContainsLessItems_Than_ExamStatus_Returns_EmptyList()
    {
        await using var fixture = new MockDbFixture();
        const int evaluationId = 1;
        const int examId = 1;
        var exam = new SpirometryExam
        {
            EvaluationId = evaluationId,
            SpirometryExamId = examId
        };
        await fixture.SharedDbContext.SpirometryExams.AddAsync(exam);
        await fixture.SharedDbContext.SaveChangesAsync();
        var cdiEvent1 = new CdiEventForPayment
        {
            EvaluationId = evaluationId,
            RequestId = Guid.NewGuid(),
            DateTime = new FakeApplicationTime().UtcNow().AddDays(-2),
            EventType = "CDIFailedEvent",
            PayProvider = false,
            Reason = "Test reason",
            ApplicationId = "DpsApp"
        };
        await fixture.SharedDbContext.CdiEventForPayments.AddAsync(cdiEvent1);
        await fixture.SharedDbContext.SaveChangesAsync();
        var examStatus1 = new ExamStatus
        {
            SpirometryExamId = examId,
            StatusCodeId = (int)StatusCode.CdiFailedWithoutPayReceived,
            StatusDateTime = cdiEvent1.DateTime.UtcDateTime
        };
        var examStatus2 = new ExamStatus
        {
            SpirometryExamId = examId,
            StatusCodeId = (int)StatusCode.CdiPassedReceived,
            StatusDateTime = new FakeApplicationTime().UtcNow().AddDays(-1)
        };
        await fixture.SharedDbContext.ExamStatuses.AddAsync(examStatus1);
        await fixture.SharedDbContext.ExamStatuses.AddAsync(examStatus2);
        await fixture.SharedDbContext.SaveChangesAsync();

        var subject = new QueryUnprocessedCdiEventForPaymentsHandler(fixture.SharedDbContext);

        var result = await subject.Handle(new QueryUnprocessedCdiEventForPayments(evaluationId), default);

        Assert.NotNull(result);
        Assert.Empty(result);
    }
    
    /// <summary>
    /// This is an invalid scenario. There should always be more items in CdiEvent table than cdi related items in ExamStatus table
    /// </summary>
    [Fact]
    public async Task Handle_When_CdiEvents_ContainsNoItems_But_ExamStatus_Have_Items_Returns_EmptyList()
    {
        await using var fixture = new MockDbFixture();
        const int evaluationId = 1;
        const int examId = 1;
        var exam = new SpirometryExam
        {
            EvaluationId = evaluationId,
            SpirometryExamId = examId
        };
        await fixture.SharedDbContext.SpirometryExams.AddAsync(exam);
        await fixture.SharedDbContext.SaveChangesAsync();
        var examStatus1 = new ExamStatus
        {
            SpirometryExamId = examId,
            StatusCodeId = (int)StatusCode.CdiFailedWithoutPayReceived,
            StatusDateTime = new FakeApplicationTime().UtcNow().AddDays(-2)
        };
        var examStatus2 = new ExamStatus
        {
            SpirometryExamId = examId,
            StatusCodeId = (int)StatusCode.CdiPassedReceived,
            StatusDateTime = new FakeApplicationTime().UtcNow().AddDays(-1)
        };
        await fixture.SharedDbContext.ExamStatuses.AddAsync(examStatus1);
        await fixture.SharedDbContext.ExamStatuses.AddAsync(examStatus2);
        await fixture.SharedDbContext.SaveChangesAsync();

        var subject = new QueryUnprocessedCdiEventForPaymentsHandler(fixture.SharedDbContext);

        var result = await subject.Handle(new QueryUnprocessedCdiEventForPayments(evaluationId), default);

        Assert.NotNull(result);
        Assert.Empty(result);
    }
}