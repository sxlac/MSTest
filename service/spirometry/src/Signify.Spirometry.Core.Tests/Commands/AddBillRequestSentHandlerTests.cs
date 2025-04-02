using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.Spirometry.Core.Commands;
using Signify.Spirometry.Core.Data.Entities;
using System.Linq;
using System.Threading.Tasks;
using System;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Commands;

public sealed class AddBillRequestSentTests : IDisposable, IAsyncDisposable
{
    private readonly MockDbFixture _dbFixture = new();

    public void Dispose()
    {
        _dbFixture.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _dbFixture.DisposeAsync();
    }

    private AddOrUpdateBillRequestSentHandler CreateSubject()
        => new(A.Dummy<ILogger<AddOrUpdateBillRequestSentHandler>>(), _dbFixture.SharedDbContext);

    [Fact]
    public async Task Handle_WhereMatchingEntityFound_DoesNotAddAnother()
    {
        // Arrange
        const int spirometryExamId = 10;
        const int evaluationId = 11;

        var expectedBillRequestSent = new BillRequestSent
        {
            BillId = Guid.NewGuid(),
            SpirometryExamId = spirometryExamId
        };

        await _dbFixture.SharedDbContext.BillRequestSents.AddAsync(expectedBillRequestSent);
        await _dbFixture.SharedDbContext.SaveChangesAsync();

        var countBillRequestSent = _dbFixture.SharedDbContext.BillRequestSents.Count();

        var request = new AddOrUpdateBillRequestSent(Guid.NewGuid(), evaluationId, expectedBillRequestSent);

        //Act
        var subject = CreateSubject();

        await subject.Handle(request, default);

        //Assert
        Assert.Equal(countBillRequestSent, _dbFixture.SharedDbContext.BillRequestSents.Count());
    }

    [Fact]
    public async Task Handle_WithNewBillRequestSent_AddsToDb()
    {
        // Arrange
        const int spirometryExamId = 10;
        const int evaluationId = 11;

        var expectedBillRequestSent = new BillRequestSent
        {
            BillId = Guid.NewGuid(),
            SpirometryExamId = spirometryExamId,
            SpirometryExam = new SpirometryExam
            {
                SpirometryExamId = spirometryExamId,
                EvaluationId = evaluationId
            }
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
}