using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.uACR.Core.Commands;
using Signify.uACR.Core.Data.Entities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.uACR.Core.Tests.Commands;

public sealed class AddExamStatusHandlerTests : IDisposable, IAsyncDisposable
{
    private readonly MockDbFixture _dbFixture = new();
    private readonly FakeApplicationTime _fakeApplicationTime = A.Fake<FakeApplicationTime>();

    public void Dispose()
    {
        _dbFixture.Dispose();
    }

    public ValueTask DisposeAsync()
        => _dbFixture.DisposeAsync();

    private AddExamStatusHandler CreateSubject()
        => new(A.Dummy<ILogger<AddExamStatusHandler>>(), _dbFixture.SharedDbContext);

    [Fact]
    public async Task Handle_WithRequest_AddsExamStatusToDatabase()
    {
        //Arrange
        await AddExam();
        var examStatus = new ExamStatus
        {
            ExamStatusCodeId = ExamStatusCode.LabResultsReceived.ExamStatusCodeId,
            StatusDateTime = _fakeApplicationTime.UtcNow(),
            CreatedDateTime = _fakeApplicationTime.UtcNow()
        };
        var request = new AddExamStatus(Guid.NewGuid(), 1, examStatus);
        var subject = CreateSubject();

        //Act
        await subject.Handle(request, CancellationToken.None);
        examStatus.ExamStatusId = 1;

        //Assert
        Assert.Single(_dbFixture.SharedDbContext.ExamStatuses);
        Assert.Equal(examStatus, _dbFixture.SharedDbContext.ExamStatuses.First());
    }

    [Fact]
    public async Task Handle_WithRequest_DoesNot_Add_ExamStatusToDatabase_When_DuplicateNotAllowed()
    {
        //Arrange
        await AddExam();
        var examStatus = new ExamStatus
        {
            ExamStatusCodeId = ExamStatusCode.LabResultsReceived.ExamStatusCodeId,
            StatusDateTime = _fakeApplicationTime.UtcNow(),
            CreatedDateTime = _fakeApplicationTime.UtcNow()
        };
        var request = new AddExamStatus(Guid.NewGuid(), 1, examStatus);
        var subject = CreateSubject();

        //Act
        await subject.Handle(request, CancellationToken.None);
        await subject.Handle(request, CancellationToken.None);
        examStatus.ExamStatusId = 1;

        //Assert
        Assert.Single(_dbFixture.SharedDbContext.ExamStatuses);
        Assert.Equal(examStatus, _dbFixture.SharedDbContext.ExamStatuses.First());
    }

    [Fact]
    public async Task Handle_WithRequest_Does_Add_ExamStatusToDatabase_When_DuplicateAllowed()
    {
        //Arrange
        await AddExam();
        var examStatus1 = new ExamStatus
        {
            ExamStatusId = 1,
            ExamStatusCodeId = ExamStatusCode.CdiPassedReceived.ExamStatusCodeId,
            StatusDateTime = _fakeApplicationTime.UtcNow(),
            CreatedDateTime = _fakeApplicationTime.UtcNow()
        };
        var examStatus2 = new ExamStatus
        {
            ExamStatusId = 2,
            ExamStatusCodeId = ExamStatusCode.CdiPassedReceived.ExamStatusCodeId,
            StatusDateTime = _fakeApplicationTime.UtcNow(),
            CreatedDateTime = _fakeApplicationTime.UtcNow()
        };
        var request1 = new AddExamStatus(Guid.NewGuid(), 1, examStatus1, true);
        var request2 = new AddExamStatus(Guid.NewGuid(), 1, examStatus2, true);
        var subject = CreateSubject();

        //Act
        await subject.Handle(request1, CancellationToken.None);
        await subject.Handle(request2, CancellationToken.None);
        examStatus1.ExamStatusId = 1;
        examStatus2.ExamStatusId = 2;

        //Assert
        Assert.Equal(2, _dbFixture.SharedDbContext.ExamStatuses.Count());
        Assert.Equal(examStatus1, _dbFixture.SharedDbContext.ExamStatuses.First());
        Assert.Equal(examStatus2, _dbFixture.SharedDbContext.ExamStatuses.Last());
    }

    private async Task AddExam()
    {
        const int evaluationId = 1;
        var previousDateTime = new DateTimeOffset(2020, 01, 01, 01, 01, 01, new TimeSpan(0, 0, 0));

        var existingExam = new Exam
        {
            EvaluationId = evaluationId,
            DateOfService = previousDateTime,
            ApplicationId = nameof(ApplicationId)
        };

        _dbFixture.SharedDbContext.Exams.Add(existingExam);
        await _dbFixture.SharedDbContext.SaveChangesAsync();
    }
}