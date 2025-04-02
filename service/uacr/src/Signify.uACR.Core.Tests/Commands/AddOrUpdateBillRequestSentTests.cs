using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.uACR.Core.Commands;
using Signify.uACR.Core.Data.Entities;
using System.Linq;
using System.Threading.Tasks;
using System;
using Signify.uACR.Core.Constants;
using Xunit;

namespace Signify.uACR.Core.Tests.Commands;

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

    private AddOrUpdateBillRequestHandler CreateSubject()
        => new(A.Dummy<ILogger<AddOrUpdateBillRequestHandler>>(), _dbFixture.SharedDbContext);

    [Fact]
    public async Task Handle_WithNewBillRequestSent_AddsToDb()
    {
        // Arrange
        const int examId = 10;
        const int evaluationId = 11;
        var expectedBillRequestSent = new BillRequest
        {
            BillId = Guid.NewGuid(),
            ExamId = examId,
            CreatedDateTime = DateTime.UtcNow,
            BillingProductCode = ProductCodes.uACR_RcmBilling
        };
        var countBillRequestSent = _dbFixture.SharedDbContext.BillRequests.Count();
        var request = new AddOrUpdateBillRequest(Guid.NewGuid(), evaluationId, expectedBillRequestSent);

        //Act
        var subject = CreateSubject();
        var actual = await subject.Handle(request, default);

        //Assert
        Assert.Equal(expectedBillRequestSent, actual);
        Assert.Equal(countBillRequestSent + 1, _dbFixture.SharedDbContext.BillRequests.Count());
    }

    [Fact]
    public async Task Handle_WithExistingBillRequestSent_UpdatesDb()
    {
        // Arrange
        const int examId = 10;
        const int evaluationId = 11;
        var billId = Guid.NewGuid();
        var newBillRequestSent = new BillRequest
        {
            BillId = billId,
            ExamId = examId,
            CreatedDateTime = _applicationTime.UtcNow()
        };
        var expectedBillRequestSent = new BillRequest
        {
            BillRequestId = 1,
            BillId = billId,
            ExamId = examId,
            CreatedDateTime = _applicationTime.UtcNow(),
            BillingProductCode = ProductCodes.uACR_RcmBilling
        };
        var request = new AddOrUpdateBillRequest(billId, evaluationId, newBillRequestSent);

        //Act
        var subject = CreateSubject();
        var addedResult = await subject.Handle(request, default);
        var countBillRequestSent = _dbFixture.SharedDbContext.BillRequests.Count();
        var updatedResult = await subject.Handle(new AddOrUpdateBillRequest(addedResult), default);

        //Assert
        Assert.Equal(expectedBillRequestSent.BillRequestId, updatedResult.BillRequestId);
        Assert.Equal(expectedBillRequestSent.BillId, updatedResult.BillId);
        Assert.Equal(expectedBillRequestSent.ExamId, updatedResult.ExamId);
        Assert.Equal(expectedBillRequestSent.CreatedDateTime, updatedResult.CreatedDateTime);
        Assert.Equal(expectedBillRequestSent.BillingProductCode, updatedResult.BillingProductCode);
        Assert.Equal(countBillRequestSent, _dbFixture.SharedDbContext.BillRequests.Count());
    }
}