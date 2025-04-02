using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Signify.CKD.Svc.Core.Data;
using Signify.CKD.Svc.Core.Data.Entities;
using Signify.CKD.Svc.Core.Queries;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.CKD.Svc.Core.Tests.Queries
{
    public class GetCKDStatusesHandlerTests
    {
        private readonly GetCKDStatusesHandler _handler;
        private CKDDataContext _contextInMemory;

        public GetCKDStatusesHandlerTests()
        {
            var options = new DbContextOptionsBuilder<CKDDataContext>().UseInMemoryDatabase(databaseName: "CKD-GetCKDStatuses").Options;
            _contextInMemory = new CKDDataContext(options);
            _handler = new GetCKDStatusesHandler(_contextInMemory);
        }

        [Fact]
        public async Task GetCKDStatusesHandler_ReturnsCKDStatuses()
        {
            // Arrange
            const int ckdId = 1;
            var query = new GetCKDStatuses() { CKDId = ckdId };

            _contextInMemory.CKDStatus.AddRange(GetCKDStatuses());
            await _contextInMemory.SaveChangesAsync();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        private static List<CKDStatus> GetCKDStatuses()
        {
            var ckd = A.Fake<Core.Data.Entities.CKD>();
            ckd.CKDId = 1;

            var result = new List<CKDStatus>()
            {
                new CKDStatus() { CKD = ckd, CKDStatusCode = CKDStatusCode.CKDPerformed },
                new CKDStatus() { CKD = ckd, CKDStatusCode = CKDStatusCode.BillableEventRecieved },
                new CKDStatus() { CKDStatusCode = CKDStatusCode.CKDNotPerformed },
            };
            return result;
        }
    }
}
