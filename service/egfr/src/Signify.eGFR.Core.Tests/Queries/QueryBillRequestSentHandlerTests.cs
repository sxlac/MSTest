using System;
using Signify.eGFR.Core.Data.Entities;
using Signify.eGFR.Core.Queries;
using System.Threading.Tasks;
using Signify.eGFR.Core.Constants;
using Xunit;

namespace Signify.eGFR.Core.Tests.Queries;

public class QueryBillRequestSentHandlerTests
{
    [Fact]
    public async Task Handle_WhenEntityDoesNotExist_EvaluationIdAndRCMProductCodeIsPresent_ReturnsNull()
    {
        const long evaluationId = 1;
        const string billingProductCode = "eGFR"; 
        await using var fixture = new MockDbFixture();
        var subject = new QueryBillRequestSentHandler(fixture.SharedDbContext);

        var result = await subject.Handle(new QueryBillRequestSent(evaluationId, billingProductCode), default);

        Assert.Null(result.Entity);
    }

    [Fact]
    public async Task Handle_WhenEntityDoesNotExist_OnlyBillIdIsPresent_ReturnsNull()
    {
        var billId = Guid.NewGuid();
        await using var fixture = new MockDbFixture();
        var subject = new QueryBillRequestSentHandler(fixture.SharedDbContext);

        var result = await subject.Handle(new QueryBillRequestSent(billId), default);

        Assert.Null(result.Entity);
    }

    [Fact]
    public async Task Handle_WhenEntityExists_EvaluationIdAndRCMProductCodeIsPresent_ReturnsEntity()
    {
        const long evaluationId = 1;
        const string billingProductCode = "eGFR"; 
        await using var fixture = new MockDbFixture();
        var exam = new Exam
        {
            EvaluationId = evaluationId,
            ApplicationId = nameof(ApplicationId)
        };
        exam = (await fixture.SharedDbContext.Exams.AddAsync(exam)).Entity;
        await fixture.SharedDbContext.SaveChangesAsync();
        var billRequestSent = new BillRequestSent
        {
            Exam = exam,
            ExamId = exam.ExamId,
            BillingProductCode = ProductCodes.eGFR_RcmBilling
        };
        await fixture.SharedDbContext.BillRequestSents.AddAsync(billRequestSent);
        await fixture.SharedDbContext.SaveChangesAsync();
        var subject = new QueryBillRequestSentHandler(fixture.SharedDbContext);

        var result = await subject.Handle(new QueryBillRequestSent(evaluationId, billingProductCode), default);

        Assert.NotNull(result.Entity);
    }

    [Fact]
    public async Task Handle_WhenEntityExists_OnlyBillIdIsPresent_ReturnsEntity()
    {
        const long evaluationId = 1;
        var billId = Guid.NewGuid();
        await using var fixture = new MockDbFixture();
        var exam = new Exam
        {
            EvaluationId = evaluationId,
            ApplicationId = nameof(ApplicationId)
        };
        exam = (await fixture.SharedDbContext.Exams.AddAsync(exam)).Entity;
        await fixture.SharedDbContext.SaveChangesAsync();
        var billRequestSent = new BillRequestSent
        {
            Exam = exam,
            BillId = billId,
            ExamId = exam.ExamId,
            BillingProductCode = ProductCodes.eGFR_RcmBilling
        };
        await fixture.SharedDbContext.BillRequestSents.AddAsync(billRequestSent);
        await fixture.SharedDbContext.SaveChangesAsync();
        var subject = new QueryBillRequestSentHandler(fixture.SharedDbContext);

        var result = await subject.Handle(new QueryBillRequestSent(billId), default);

        Assert.NotNull(result.Entity);
    }
}