using Microsoft.EntityFrameworkCore;
using Signify.DEE.Core.Messages.Queries;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using System.Threading.Tasks;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Messages.Queries
{
    public class GetHoldHandlerTests
    {
        [Fact]
        public async Task Handle_Test()
        {
            // Arrange
            const int evaluationId = 567;

            var request = new GetHold
            {
                EvaluationId = evaluationId
            };

            var opt = new DbContextOptionsBuilder<DataContext>().UseInMemoryDatabase("GetHold").Options;
            var db = new DataContext(opt);

            await db.Holds.AddAsync(new Hold
            {
                EvaluationId = evaluationId,
                HoldId = 382
            });

            await db.SaveChangesAsync();

            var subject = new GetHoldHandler(db);

            // Act
            var actual = await subject.Handle(request, default);

            // Assert
            Assert.NotNull(actual);
        }
    }
}
