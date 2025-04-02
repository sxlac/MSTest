using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.eGFR.Core.Commands;
using Signify.eGFR.Core.Data.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;
using Signify.eGFR.Core.Constants;
using Xunit;

namespace Signify.eGFR.Core.Tests.Commands;

public sealed class AddOrUpdateBillRequestSentTests : IDisposable, IAsyncDisposable
{
    private readonly MockDbFixture _dbFixture = new();
    private readonly FakeApplicationTime _applicationTime = new();

    public void Dispose()
    {
        _dbFixture.Dispose();
    }

    public ValueTask DisposeAsync()
        => _dbFixture.DisposeAsync();

    private AddOrUpdateBillRequestSentHandler CreateSubject()
        => new(A.Dummy<ILogger<AddOrUpdateBillRequestSentHandler>>(), _dbFixture.SharedDbContext);

    [Fact]
    public async Task Handle_WithNewBillRequestSent_AddsToDb()
    {
        // Arrange
        const int examId = 10;
        const int evaluationId = 11;
        var expectedBillRequestSent = new BillRequestSent
        {
            BillId = Guid.NewGuid(),
            ExamId = examId,
            CreatedDateTime = DateTime.UtcNow,
            BillingProductCode = ProductCodes.eGFR_RcmBilling
        };
        var countBillRequestSent = _dbFixture.SharedDbContext.BillRequestSents.Count();
        var request = new AddOrUpdateBillRequestSent(Guid.NewGuid(), evaluationId, expectedBillRequestSent);

        //Act
        var subject = CreateSubject();
        var actual = await subject.Handle(request, default);

        //Assert
        Assert.Equal(expectedBillRequestSent, actual);
        Assert.Equal(countBillRequestSent + 1, _dbFixture.SharedDbContext.BillRequestSents.Count());
    }

    [Fact]
    public async Task Handle_WithExistingBillRequestSent_UpdatesDb()
    {
        // Arrange
        const int examId = 10;
        const int evaluationId = 11;
        var billId = Guid.NewGuid();
        var newBillRequestSent = new BillRequestSent
        {
            BillId = billId,
            ExamId = examId,
            CreatedDateTime = _applicationTime.UtcNow()
        };
        var expectedBillRequestSent = new BillRequestSent
        {
            BillRequestSentId = 1,
            BillId = billId,
            ExamId = examId,
            CreatedDateTime = _applicationTime.UtcNow(),
            Accepted = true,
            AcceptedAt = _applicationTime.UtcNow(),
            BillingProductCode = ProductCodes.eGFR_RcmBilling
        };
        var request = new AddOrUpdateBillRequestSent(billId, evaluationId, newBillRequestSent);

        //Act
        var subject = CreateSubject();
        var addedResult = await subject.Handle(request, default);
        var countBillRequestSent = _dbFixture.SharedDbContext.BillRequestSents.Count();
        addedResult.Accepted = true;
        addedResult.AcceptedAt = _applicationTime.UtcNow();
        var updatedResult = await subject.Handle(new AddOrUpdateBillRequestSent(addedResult), default);

        //Assert
        Assert.Equal(expectedBillRequestSent.BillRequestSentId, updatedResult.BillRequestSentId);
        Assert.Equal(expectedBillRequestSent.BillId, updatedResult.BillId);
        Assert.Equal(expectedBillRequestSent.ExamId, updatedResult.ExamId);
        Assert.Equal(expectedBillRequestSent.Accepted, updatedResult.Accepted);
        Assert.Equal(expectedBillRequestSent.AcceptedAt, updatedResult.AcceptedAt);
        Assert.Equal(expectedBillRequestSent.CreatedDateTime, updatedResult.CreatedDateTime);
        Assert.Equal(expectedBillRequestSent.BillingProductCode, updatedResult.BillingProductCode);
        Assert.Equal(countBillRequestSent, _dbFixture.SharedDbContext.BillRequestSents.Count());
    }
}