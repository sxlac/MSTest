using System;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Signify.Dps.Observability.Library.Services;
using Signify.uACR.Core.Commands;
using Signify.uACR.Core.Data.Entities;
using UacrNsbEvents;
using Xunit;

namespace Signify.uACR.Core.Tests.Commands;

public sealed class UpdateExamHandlerTests : IDisposable, IAsyncDisposable
    {
        private readonly MockDbFixture _fixture = new();
        private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();
        private readonly FakeApplicationTime _fakeApplicationTime = A.Fake<FakeApplicationTime>();

        public void Dispose()
        {
            _fixture.Dispose();
        }

        public ValueTask DisposeAsync()
            => _fixture.DisposeAsync();

        [Fact]
        public async Task Handle_WithEvaluationNotInDatabase_Throws()
        {
            var evalReceived = new EvalReceived
            {
                EvaluationId = 5,
                DateOfService = _fakeApplicationTime.UtcNow()
            };

            var subject = new UpdateExamHandler(A.Dummy<ILogger<UpdateExamHandler>>(),
                _fixture.SharedDbContext, _publishObservability);

            await Assert.ThrowsAnyAsync<InvalidOperationException>(async () => await subject.Handle(new UpdateExam(evalReceived),
                CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WithNullDateOfService_DoesNothing()
        {
            const long evaluationId = 1;

            var previousDateTime = new DateTime(2020, 01, 01, 01, 01, 01, DateTimeKind.Utc);

            var existingExam = new Exam
            {
                EvaluationId = evaluationId,
                DateOfService = previousDateTime,
                ApplicationId = nameof(ApplicationId)
            };

            _fixture.SharedDbContext.Exams.Add(existingExam);
            await _fixture.SharedDbContext.SaveChangesAsync();

            var request = new UpdateExam(new EvalReceived
            {
                EvaluationId = evaluationId,
                DateOfService = null
            });

            //Act
            var subject = new UpdateExamHandler(A.Dummy<ILogger<UpdateExamHandler>>(),
                _fixture.SharedDbContext, _publishObservability);

            await subject.Handle(request, CancellationToken.None);

            Assert.Single(_fixture.SharedDbContext.Exams);

            var exam = await _fixture.SharedDbContext.Exams.FirstAsync(
                exams => exams.EvaluationId == evaluationId);

            Assert.Equal(previousDateTime, exam.DateOfService);
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
            const long evaluationId = 1;

            var previousDateTime = new DateTime(2020, 01, 01, 01, 01, 01, DateTimeKind.Utc);
            var updatedDateTime = previousDateTime.AddDays(daysDiff);

            var existingExam = new Exam
            {
                EvaluationId = evaluationId,
                DateOfService = previousDateTime,
                ApplicationId = nameof(ApplicationId)
            };

            _fixture.SharedDbContext.Exams.Add(existingExam);
            await _fixture.SharedDbContext.SaveChangesAsync();

            var request = new UpdateExam(new EvalReceived
            {
                EvaluationId = evaluationId,
                DateOfService = updatedDateTime
            });

            //Act
            var subject = new UpdateExamHandler(A.Dummy<ILogger<UpdateExamHandler>>(),
                _fixture.SharedDbContext, _publishObservability);

            var result = await subject.Handle(request, CancellationToken.None);

            //Assert
            if (shouldBeUpdated)
            {
                Assert.NotNull(result);

                var exam = await _fixture.SharedDbContext.Exams.FirstAsync(
                    exams => exams.EvaluationId == evaluationId);

                Assert.Equal(updatedDateTime, exam.DateOfService);
            }
            else
            {
                Assert.Null(result);
            }
        }
    }