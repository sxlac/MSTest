using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Messages.Commands;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Messages.Commands
{
    public sealed class CreateHoldHandlerTests
    {
        private DataContext ctx { get; set; }

        public CreateHoldHandlerTests()
        {
            var opt = new DbContextOptionsBuilder<DataContext>().UseInMemoryDatabase("CreateHold").Options;
            ctx = new DataContext(opt);
        }

        private CreateHoldHandler CreateSubject()
            => new(A.Dummy<ILogger<CreateHoldHandler>>(), ctx);

        [Fact]
        public async Task Handle_WithNewHold_AddsHoldToDatabase()
        {
            // Arrange
            const int evaluationId = 435;

            var hold = new Hold
            {
                EvaluationId = evaluationId
            };

            var request = new CreateHold(hold);

            var subject = CreateSubject();

            var existingHoldCount = await ctx.Holds.CountAsync();

            // Act
            var actualResult = await subject.Handle(request, CancellationToken.None);

            // Assert
            Assert.True(actualResult.IsNew);
            Assert.Equal(hold, actualResult.Hold);

            Assert.Equal(existingHoldCount + 1, await ctx.Holds.CountAsync());

            var insertedHold = await ctx.Holds.SingleOrDefaultAsync(each => each.EvaluationId == evaluationId);

            Assert.Equal(hold, insertedHold);
        }

        [Fact]
        public async Task Handle_WithDuplicateHold_DoesNothing()
        {
            // Arrange
            const int evaluationId = 434;

            var existingHold = new Hold
            {
                EvaluationId = evaluationId,
                CdiHoldId = Guid.NewGuid()
            };

            await ctx.Holds.AddAsync(existingHold);
            await ctx.SaveChangesAsync();

            var request = new CreateHold(new Hold
            {
                EvaluationId = evaluationId,
                CdiHoldId = Guid.NewGuid()
            });

            var subject = CreateSubject();

            var existingHoldCount = await ctx.Holds.CountAsync();

            // Act
            var actualResult = await subject.Handle(request, CancellationToken.None);

            // Assert
            Assert.False(actualResult.IsNew);
            Assert.Equal(existingHold, actualResult.Hold);

            Assert.Equal(existingHoldCount, await ctx.Holds.CountAsync());
        }
    }
}
