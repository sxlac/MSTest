using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Exceptions;
using Signify.DEE.Svc.Core.Messages.Commands;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Messages.Commands
{
    public sealed class UpdateHoldHandlerTests
    {
        private DataContext ctx { get; set; }

        public UpdateHoldHandlerTests()
        {
            var opt = new DbContextOptionsBuilder<DataContext>().UseInMemoryDatabase("UpdateHold").Options;
            ctx = new DataContext(opt);
        }

        private UpdateHoldHandler CreateSubject()
            => new(A.Dummy<ILogger<UpdateHoldHandler>>(), ctx);

        [Fact]
        public async Task Handle_WithNotReleased_UpdatesHold()
        {
            // Arrange
            const int evaluationId = 333;
            const int holdId = 433;
            var cdiHoldId = Guid.NewGuid();

            var releasedDateTime = DateTime.UtcNow;

            var existingHold = new Hold
            {
                EvaluationId = evaluationId,
                HoldId = holdId,
                CdiHoldId = cdiHoldId
            };

            await ctx.Holds.AddAsync(existingHold);
            await ctx.SaveChangesAsync();

            var request = new UpdateHold(cdiHoldId, evaluationId, releasedDateTime);

            var subject = CreateSubject();

            // Act
            var actualResult = await subject.Handle(request, CancellationToken.None);

            // Assert
            Assert.False(actualResult.IsNoOp);
            Assert.Equal(releasedDateTime, actualResult.Hold.ReleasedDateTime);
        }

        [Fact]
        public async Task Handle_WhenHoldAlreadyReleased_DoesNothing()
        {
            // Arrange
            const int evaluationId = 334;
            var cdiHoldId = Guid.NewGuid();
            var previouslyReleasedTimestamp = DateTime.UtcNow;

            var hold = new Hold
            {
                EvaluationId = evaluationId,
                CdiHoldId = cdiHoldId,
                ReleasedDateTime = previouslyReleasedTimestamp
            };

            await ctx.Holds.AddAsync(hold);
            await ctx.SaveChangesAsync();

            var request = new UpdateHold(cdiHoldId, evaluationId, DateTime.UtcNow);

            var subject = CreateSubject();

            // Act
            var result = await subject.Handle(request, CancellationToken.None);

            // Assert
            Assert.True(result.IsNoOp);
            Assert.Equal(previouslyReleasedTimestamp, result.Hold.ReleasedDateTime);
            Assert.Equal(hold, result.Hold);
        }

        [Fact]
        public async Task Handle_WhenHoldNotFoundForEvaluation_Throws()
        {
            // Arrange
            var request = new UpdateHold(Guid.NewGuid(), 1, DateTime.UtcNow);

            var subject = CreateSubject();

            // Act
            // Assert
            await Assert.ThrowsAnyAsync<HoldNotFoundException>(async () =>
                await subject.Handle(request, CancellationToken.None));
        }
    }
}
