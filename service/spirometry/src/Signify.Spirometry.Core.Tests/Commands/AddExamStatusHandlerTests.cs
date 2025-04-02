using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.Spirometry.Core.Commands;
using Signify.Spirometry.Core.Data.Entities;
using System.Threading.Tasks;
using System;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Commands;

public sealed class AddExamStatusHandlerTests : IDisposable, IAsyncDisposable
{
    private readonly MockDbFixture _dbFixture = new ();

    public void Dispose()
    {
        _dbFixture.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _dbFixture.DisposeAsync();
    }

    private AddExamStatusHandler CreateSubject()
        => new (A.Dummy<ILogger<AddExamStatusHandler>>(), _dbFixture.SharedDbContext);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_WhenStatusDoesNotExistForExam_ReturnsNewEntity(bool alwaysAdd)
    {
        var request = new AddExamStatus(Guid.Empty, default, new ExamStatus
        {
            SpirometryExamId = 1,
            StatusCodeId = StatusCode.SpirometryExamPerformed.StatusCodeId
        }, alwaysAdd);

        var subject = CreateSubject();

        var actual = await subject.Handle(request, default);

        Assert.NotNull(actual);
        Assert.Equal(request.Status, actual.Status);
        Assert.True(actual.IsNew);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_WhenStatusAlreadyExistsForEvaluation_ReturnsExistingEntity(bool alwaysAdd)
    {
        var existingStatus = new ExamStatus
        {
            SpirometryExamId = 1,
            StatusCodeId = StatusCode.CdiPassedReceived.StatusCodeId
        };

        await _dbFixture.SharedDbContext.ExamStatuses.AddAsync(existingStatus);
        await _dbFixture.SharedDbContext.SaveChangesAsync();
        var newStatus = new ExamStatus
        {
            SpirometryExamId = 1,
            ExamStatusId = 2,
            StatusCodeId = StatusCode.CdiPassedReceived.StatusCodeId
        };
        var request = new AddExamStatus(Guid.Empty, default, new ExamStatus
        {
            SpirometryExamId = existingStatus.SpirometryExamId,
            StatusCodeId = existingStatus.StatusCodeId
        }, alwaysAdd);

        var subject = CreateSubject();

        var actual = await subject.Handle(request, default);

        Assert.NotNull(actual);
        if(alwaysAdd)
        {
            Assert.NotEqual(existingStatus, actual.Status);
            Assert.Equal(newStatus, actual.Status);
        }
        else
        {
            Assert.Equal(existingStatus, actual.Status);
        }
        Assert.Equal(alwaysAdd, actual.IsNew);
    }
}