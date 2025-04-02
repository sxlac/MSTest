using Signify.uACR.Core.Data.Entities;
using Signify.uACR.Core.Queries;
using System.Threading.Tasks;
using System;
using Signify.uACR.Core.Constants;
using Xunit;

namespace Signify.uACR.Core.Tests.Queries;

public class QueryBillRequestSentHandlerTests
{
    [Fact]
    public async Task Handle_WhenEntityDoesNotExist_EvaluationIdAndRCMProductCodeIsPresent_ReturnsNull()
    {
        const long evaluationId = 1;
        const string billingProductCode = "uACR"; 
        
        await using var fixture = new MockDbFixture();

        var subject = new QueryBillRequestsHandler(fixture.SharedDbContext);

        var result = await subject.Handle(new QueryBillRequests(evaluationId, billingProductCode), default);

        Assert.Null(result.Entity);
    }
        
    [Fact]
    public async Task Handle_WhenEntityDoesNotExist_OnlyBillIdIsPresent_ReturnsNull()
    {
        var billId = Guid.NewGuid();

        await using var fixture = new MockDbFixture();

        var subject = new QueryBillRequestsHandler(fixture.SharedDbContext);

        var result = await subject.Handle(new QueryBillRequests(billId), default);

        Assert.Null(result.Entity);
    }

    [Fact]
    public async Task Handle_WhenEntityExists_EvaluationIdAndRCMProductCodeIsPresent_ReturnsEntity()
    {
        const long evaluationId = 1;
        const string billingProductCode = "uACR"; 
        
        await using var fixture = new MockDbFixture();

        var exam = new Exam
        {
            EvaluationId = evaluationId,
            ApplicationId = nameof(ApplicationId)
        };

        exam = (await fixture.SharedDbContext.Exams.AddAsync(exam)).Entity;
        await fixture.SharedDbContext.SaveChangesAsync();

        var billRequestSent = new BillRequest
        {
            Exam = exam,
            ExamId = exam.ExamId,
            BillingProductCode = ProductCodes.uACR_RcmBilling
        };

        await fixture.SharedDbContext.BillRequests.AddAsync(billRequestSent);
        await fixture.SharedDbContext.SaveChangesAsync();

        var subject = new QueryBillRequestsHandler(fixture.SharedDbContext);

        var result = await subject.Handle(new QueryBillRequests(evaluationId, billingProductCode), default);

        Assert.NotNull(result.Entity);
    }
    
    [Fact]
    public async Task Handle_WhenEntityExists_OnlyBillIdIsPresent_ReturnsEntity()
    { 
        long evaluationId = 1;
        var billId = Guid.NewGuid();

        await using var fixture = new MockDbFixture();

        var exam = new Exam
        {
            EvaluationId = evaluationId,
            ApplicationId = nameof(ApplicationId)
        };

        exam = (await fixture.SharedDbContext.Exams.AddAsync(exam)).Entity;
        await fixture.SharedDbContext.SaveChangesAsync();

        var billRequestSent = new BillRequest
        {
            Exam = exam,
            BillId = billId,
            ExamId = exam.ExamId,
            BillingProductCode = ProductCodes.uACR_RcmBilling
        };

        await fixture.SharedDbContext.BillRequests.AddAsync(billRequestSent);
        await fixture.SharedDbContext.SaveChangesAsync();

        var subject = new QueryBillRequestsHandler(fixture.SharedDbContext);

        var result = await subject.Handle(new QueryBillRequests(billId), default);

        Assert.NotNull(result.Entity);
    }
}