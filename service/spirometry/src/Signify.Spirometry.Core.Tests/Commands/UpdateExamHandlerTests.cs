using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using Signify.Spirometry.Core.Commands;
using Signify.Spirometry.Core.Data.Entities;
using SpiroNsbEvents;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Commands;

public sealed class UpdateExamHandlerTests : IDisposable, IAsyncDisposable
{
    private readonly MockDbFixture _fixture = new();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();
        
    public void Dispose()
    {
        _fixture.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _fixture.DisposeAsync();
    }

    [Fact]
    public async Task Handle_WithEvaluationNotInDatabase_Throws()
    {
        var evalReceived = new EvalReceived
        {
            EvaluationId = 5,
            DateOfService = DateTime.UtcNow
        };

        var subject = new UpdateExamHandler(A.Dummy<ILogger<UpdateExamHandler>>(),
            _fixture.SharedDbContext,
            _publishObservability);

        await Assert.ThrowsAnyAsync<InvalidOperationException>(async () => await subject.Handle(new UpdateExam(evalReceived),
            CancellationToken.None));
            
        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>._, true)).MustNotHaveHappened();
    }

    [Fact]
    public async Task Handle_WithNullDateOfService_DoesNothing()
    {
        const int evaluationId = 1;

        var previousDateTime = new DateTime(2020, 01, 01, 01, 01, 01, DateTimeKind.Utc);

        var existingExam = new SpirometryExam
        {
            EvaluationId = evaluationId,
            DateOfService = previousDateTime
        };

        _fixture.SharedDbContext.SpirometryExams.Add(existingExam);
        await _fixture.SharedDbContext.SaveChangesAsync();

        var request = new UpdateExam(new EvalReceived
        {
            EvaluationId = evaluationId,
            DateOfService = null
        });

        //Act
        var subject = new UpdateExamHandler(A.Dummy<ILogger<UpdateExamHandler>>(),
            _fixture.SharedDbContext,
            _publishObservability);

        await subject.Handle(request, CancellationToken.None);

        Assert.Single(_fixture.SharedDbContext.SpirometryExams);

        var exam = await _fixture.SharedDbContext.SpirometryExams.FirstAsync(
            exams => exams.EvaluationId == evaluationId);

        Assert.Equal(previousDateTime, exam.DateOfService);
            
        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>._, true)).MustNotHaveHappened();
    }

    /// <param name="daysDiff">How many days after the original DateOfService the updated DateOfService should be</param>
    /// <param name="shouldBeUpdated">Whether or not the record should be updated</param>
    [Theory]
    [InlineData(3, true)]
    [InlineData(-3, true)]
    [InlineData(0, false)]
    public async Task Handle_WithDateOfService_Tests(int daysDiff, bool shouldBeUpdated)
    {
        // Arrange
        const int evaluationId = 1;

        var previousDateTime = new DateTime(2020, 01, 01, 01, 01, 01, DateTimeKind.Utc);
        var updatedDateTime = previousDateTime.AddDays(daysDiff);

        var existingExam = new SpirometryExam
        {
            EvaluationId = evaluationId,
            DateOfService = previousDateTime
        };

        _fixture.SharedDbContext.SpirometryExams.Add(existingExam);
        await _fixture.SharedDbContext.SaveChangesAsync();

        var request = new UpdateExam(new EvalReceived
        {
            EvaluationId = evaluationId,
            DateOfService = updatedDateTime
        });

        //Act
        var subject = new UpdateExamHandler(A.Dummy<ILogger<UpdateExamHandler>>(),
            _fixture.SharedDbContext,
            _publishObservability);

        var result = await subject.Handle(request, CancellationToken.None);

        //Assert
        if (shouldBeUpdated)
        {
            Assert.NotNull(result);

            var exam = await _fixture.SharedDbContext.SpirometryExams.FirstAsync(
                exams => exams.EvaluationId == evaluationId);

            Assert.Equal(updatedDateTime, exam.DateOfService);
                
            A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>._, true)).MustHaveHappenedOnceExactly();
        }
        else
        {
            Assert.Null(result);
        }
    }
}